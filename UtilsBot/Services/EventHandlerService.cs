using System.Globalization;
using Discord;
using Discord.WebSocket;
using UtilsBot.Datenbank;
using UtilsBot.Domain;
using UtilsBot.Domain.MessageSent;
using UtilsBot.Domain.Xp;
using UtilsBot.Domain.XpLeaderboard;
using UtilsBot.Repository;

namespace UtilsBot.Services;

public class EventHandlerService : HelperService
{
    private readonly DiscordSocketClient _client;
    private readonly LevelService _levelService;
    private readonly EmbedFactory _embedFactory;
    private readonly CommandRegistrationService _commandRegistrationService;
    private readonly WarEraContractMonitor _warEraContractMonitor;
    private readonly MessageService _messageService;
    private readonly RoleService _roleService;

    public EventHandlerService(
        DiscordSocketClient client,
        LevelService levelService,
        EmbedFactory embedFactory,
        CommandRegistrationService commandRegistrationService,
        WarEraContractMonitor warEraContractMonitor)
    {
        _client = client;
        _levelService = levelService;
        _embedFactory = embedFactory;
        _commandRegistrationService = commandRegistrationService;
        _warEraContractMonitor = warEraContractMonitor;
        _roleService = new RoleService();
        _messageService= new MessageService();
    }

    public void RegisterEventHandlers()
    {
        _client.SlashCommandExecuted += SlashCommandHandlerAsync;
        _client.MessageReceived += HandleMessageReceived;
    }

    private async Task HandleMessageReceived(SocketMessage message)
    {
        await using var db = new DatabaseRepository(new BotDbContext());
        if (message.Author.IsBot) return;
        await _messageService.HandleRequest(new MessageSentRequest(message.Author.Id, message), db);
    }
    
    private async Task DeleteSlashCommands(SocketMessage message)
    {
        foreach (var guild in message.Author.MutualGuilds)
        {
            var commands = _client.Rest.GetGuildApplicationCommands(guild.Id).Result;
            foreach (var command in commands)
            {
                await command.DeleteAsync();
            }
        }

        await message.DeleteAsync();
    }

    public async void RegisterCommands()
    {
        await _commandRegistrationService.RegisterCommands();
    }

    private static T GetOptionValue<T>(SocketSlashCommand command, string name, T defaultValue = default)
    {
        var option = command.Data.Options.FirstOrDefault(x => x.Name == name)?.Value;
        if (option == null) return defaultValue;
        return (T)option;
    }

    // Rates are entered locale-independently: accept "0.11" or "0,11" alike.
    private static decimal? ParseRate(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var normalized = input.Trim().Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    // Damage is an integer; treat '.', ',' and spaces purely as grouping separators.
    private static long? ParseDamage(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var digits = new string(input.Where(char.IsDigit).ToArray());
        return long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static List<string> GetInvalidCountryCodes(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return new List<string>();

        return input
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(code => code.Length != 2 || !code.All(char.IsLetter))
            .ToList();
    }

    private static string NormalizeCountryCodes(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        return string.Join(",",
            input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(code => code.Length == 2 && code.All(char.IsLetter))
                .Select(code => code.ToUpperInvariant())
                .Distinct());
    }

    private async Task SlashCommandHandlerAsync(SocketSlashCommand command)
    {
        await using var db = new DatabaseRepository(new BotDbContext());

        if (command.CommandName == "info")
        {
            var transparenz = GetOptionValue<string>(command, "transparenz");
            var ephimeral = transparenz == "transparent";
            await InfoResponse(command, !ephimeral, db);
        }

        if (command.CommandName == "leaderboardxp")
        {
            var transparenz = GetOptionValue<string>(command, "transparenz");
            var ephemeral = transparenz == "transparent";
            await LeaderboardXpResponse(command, !ephemeral, db);
        }

        if (command.CommandName == "warerasubscribe")
        {
            if (command.User is SocketGuildUser guildUser)
            {
                await db.UpsertSubscriptionAsync(guildUser.Guild.Id, command.ChannelId ?? 0);
                await command.RespondAsync("Successfully subscribed this channel to WarEra contract notifications!", ephemeral: true);
            }
        }

        if (command.CommandName == "startsearch")
        {
            if (command.User is SocketGuildUser guildUser)
            {
                var minRate = ParseRate(GetOptionValue<string>(command, "minrate"));
                var maxDamage = ParseDamage(GetOptionValue<string>(command, "maxdamage"));

                bool? pro = null;
                var proOption = command.Data.Options.FirstOrDefault(x => x.Name == "pro")?.Value;
                if (proOption is bool b) pro = b;

                await db.SetSubscriptionStateAsync(guildUser.Guild.Id, true, minRate, maxDamage, pro);

                var parts = new List<string>();
                if (minRate.HasValue) parts.Add($"min rate: {minRate.Value:0.###} / 1k dmg");
                if (maxDamage.HasValue) parts.Add($"max damage: {maxDamage.Value:N0}");
                if (pro.HasValue) parts.Add($"pro contracts: {(pro.Value ? "included" : "skipped")}");
                var msg = parts.Count > 0
                    ? $"WarEra contract notifications started ({string.Join(", ", parts)})."
                    : "WarEra contract notifications started for this guild.";
                await command.RespondAsync(msg, ephemeral: true);
            }
        }

        if (command.CommandName == "wareraexcludedcountries")
        {
            if (command.User is SocketGuildUser guildUser)
            {
                var countriesInput = GetOptionValue<string>(command, "countries");
                var invalidCountryCodes = GetInvalidCountryCodes(countriesInput);
                if (invalidCountryCodes.Any())
                {
                    await command.RespondAsync(
                        $"Invalid country code(s): {string.Join(", ", invalidCountryCodes)}. Use two-letter codes like `DE,FR,US`.",
                        ephemeral: true);
                    return;
                }

                var excludedCountries = NormalizeCountryCodes(countriesInput);
                var updated = await db.SetExcludedTargetCountryCodesAsync(guildUser.Guild.Id, excludedCountries);
                if (!updated)
                {
                    await command.RespondAsync("Subscribe this guild first with `/warerasubscribe`.", ephemeral: true);
                    return;
                }

                var msg = string.IsNullOrEmpty(excludedCountries)
                    ? "Excluded target countries cleared."
                    : $"Excluded target countries set to: {excludedCountries}.";
                await command.RespondAsync(msg, ephemeral: true);
            }
        }

        if (command.CommandName == "endsearch")
        {
            if (command.User is SocketGuildUser guildUser)
            {
                await db.SetSubscriptionStateAsync(guildUser.Guild.Id, false);
                await command.RespondAsync("WarEra contract notifications stopped for this guild.", ephemeral: true);
            }
        }

        if (command.CommandName == "warerasetapikey")
        {
            var apiKey = GetOptionValue<string>(command, "apikey");
            if (!string.IsNullOrEmpty(apiKey))
            {
                await db.SetSettingAsync("WarEraApiKey", apiKey);
                ApplicationState.WarEraApiKey = apiKey;
                await command.RespondAsync("WarEra API key has been updated successfully.", ephemeral: true);
            }
            else
            {
                await command.RespondAsync("Invalid API key provided.", ephemeral: true);
            }
        }

        if (command.CommandName == "wareratestnotify")
        {
            await command.DeferAsync(ephemeral: true);
            var channel = await _client.GetChannelAsync(command.ChannelId ?? 0) as IMessageChannel;
            if (channel == null)
            {
                await command.FollowupAsync("Could not resolve this channel.", ephemeral: true);
            }
            else
            {
                var result = await _warEraContractMonitor.SendTestNotificationAsync(channel);
                await command.FollowupAsync(result, ephemeral: true);
            }
        }

        if (command.CommandName == "warerastatus")
        {
            if (command.User is SocketGuildUser guildUser)
            {
                var sub = await db.GetSubscriptionAsync(guildUser.Guild.Id);
                var apiKeySet = !string.IsNullOrEmpty(ApplicationState.WarEraApiKey);
                
                var status = $"**WarEra Monitor Status**\n" +
                             $"- API Key Set: {(apiKeySet ? "✅" : "❌")}\n" +
                             $"- Subscribed: {(sub != null ? "✅" : "❌")}\n" +
                             $"- Notifications Enabled: {(sub?.IsEnabled == true ? "✅" : "❌")}\n" +
                             $"- Channel: {(sub != null ? $"<#{sub.ChannelId}>" : "N/A")}\n" +
                             $"- Min Rate: {(sub != null ? $"{sub.MinimumRate:0.###} / 1k dmg" : "N/A")}\n" +
                             $"- Max Damage: {(sub != null && sub.MaximumDamage > 0 ? $"{sub.MaximumDamage:N0}" : sub != null ? "No limit" : "N/A")}\n" +
                             $"- Pro Contracts: {(sub != null ? (sub.IncludeProContracts ? "✅ included" : "❌ skipped") : "N/A")}\n" +
                             $"- Excluded Target Countries: {(sub != null && !string.IsNullOrEmpty(sub.ExcludedTargetCountryCodes) ? sub.ExcludedTargetCountryCodes : sub != null ? "None" : "N/A")}";
                
                await command.RespondAsync(status, ephemeral: true);
            }
        }
    }

    private async Task LeaderboardXpResponse(SocketSlashCommand command, bool invisibleMessage, DatabaseRepository db)
    {
        if (command.User is SocketGuildUser guildUser)
        {
            await command.DeferAsync(ephemeral: invisibleMessage);
            var leaderboardResponse =
                await _levelService.HandleRequest(new XpLeaderboardRequest(guildUser.Guild.Id), db);

            var embedBuilder = new EmbedBuilder()
                .WithTitle("XP Leaderboard")
                .WithColor(Color.DarkRed);

            for (int i = 0; i < leaderboardResponse.personen.Count; i++)
            {
                embedBuilder
                    .AddField($"Platz {i + 1}:",
                        $"```(LVL {_levelService.BerechneLevelUndRestXp(ToIntDirect(leaderboardResponse.personen[i].Xp))}) {leaderboardResponse.personen[i].DisplayName} ```");
            }

            var embed = embedBuilder.Build();


            var followupMessage = await command.FollowupAsync(embed: embed, ephemeral: invisibleMessage);
            if (!invisibleMessage)
            {
                NachrichtenLoeschenNachXSekunden(followupMessage);
            }
        }
    }

    private async Task InfoResponse(SocketSlashCommand command, bool invisibleMessage, DatabaseRepository db)
    {
        if (command.User is SocketGuildUser guildUser)
        {
            await command.DeferAsync(ephemeral: invisibleMessage);
            var xpResponse =
                await _levelService.HandleRequest(
                    new XpRequest(guildUser.Id, guildUser.DisplayName, guildUser.Guild.Id), db);

            var embed = await _embedFactory.BuildInfoEmbed(xpResponse);

            var followupMessage = await command.FollowupAsync(embed: embed, ephemeral: invisibleMessage);
            if (!invisibleMessage)
            {
                NachrichtenLoeschenNachXSekunden(followupMessage);
            }
        }
    }

    private void NachrichtenLoeschenNachXSekunden(IUserMessage sendTask, int sekunden = 300)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(sekunden));
            await sendTask.Channel.DeleteMessageAsync(sendTask.Id);
        });
    }
}
