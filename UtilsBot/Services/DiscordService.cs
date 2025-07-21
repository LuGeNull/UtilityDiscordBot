using System.Net.Mime;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using UtilsBot.Domain;
using UtilsBot.Domain.Contracts;
using UtilsBot.Domain.Models;

namespace UtilsBot.Services;

public class DiscordService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordSocketClient _client;
    private readonly string _token;
    private readonly BotConfig _config;
    private readonly VoiceChannelChangeListenerService _listener;

    public DiscordService(IOptions<BotConfig> config, IOptions<Secrets> secrets, IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _token = secrets.Value.DiscordToken;
        _config = config.Value;
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        });
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _listener = new VoiceChannelChangeListenerService(scopeFactory, config.Value);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.LogoutAsync();
        await _client.StopAsync();
    }

    private async Task ReadyAsync()
    {
        Console.WriteLine($"Online as: {_client.CurrentUser}");

        _client.MessageReceived += message =>
        {
            if (message.Author.IsBot) return Task.CompletedTask;

            Console.WriteLine($"Nachricht von {message.Author.Username}: {message.Content}");
            return Task.CompletedTask;
        };

        _client.SlashCommandExecuted += SlashCommandHandlerAsync;

        await RegistriereCommands(_client);

        await _listener.StartPeriodicCheck(_client);
    }

    private async Task RegistriereCommands(DiscordSocketClient client)
    {
        foreach (var guildId in client.Guilds.Select(g => g.Id))
        {
            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("interested")
                .WithDescription("Bekomme Benachrichtigungen wenn Personen einen Discord Channel beitreten")
                .AddOption("von", ApplicationCommandOptionType.Integer, "Von (z.B Wert:15 für ab 15 Uhr Nachmittags)",
                    true)
                .AddOption("bis", ApplicationCommandOptionType.Integer, "Bis (z.B Wert:23 für bis 23 Uhr Abends)", true)
                .Build(), guildId);

            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("info")
                .WithDescription("Auskunft über deinen Fortschritt")
                .Build(), guildId);
        }
    }

    private async Task SlashCommandHandlerAsync(SocketSlashCommand command)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBotRepository>();
        
        if (command.CommandName == "interested")
        {
            if (!_config.KommandosAktiviert)
            {
                return;
            }

            var von = (long?)command.Data.Options.FirstOrDefault(x => x.Name == "von")?.Value ?? 0L;
            var bis = (long?)command.Data.Options.FirstOrDefault(x => x.Name == "bis")?.Value ?? 0L;
            if (von == bis)
            {
                von = 0;
                bis = 24;
            }

            if (von < 0 || von > 24 || bis < 0 || bis > 24)
            {
                await command.RespondAsync("Bitte gib Werte zwischen 0 und 24 an.", ephemeral: true);
                return;
            }

            if (command.User is SocketGuildUser guildUser)
            {
                repository.AddUserToInterestedList(
                    guildUser.Id, guildUser.DisplayName, guildUser.Guild.Id, von, bis);
                await command.RespondAsync("I'll notify you!", ephemeral: true);
            }
        }


        if (command.CommandName == "info")
        {
            if (!_config.KommandosAktiviert)
            {
                return;
            }

            if (command.User is SocketGuildUser guildUser)
            {
                await command.DeferAsync(ephemeral: true);
                int startXp = 1000;
                double faktor = 1.3;
                AllgemeinePerson person = repository.HoleUserMitId(guildUser.Id);
                long xp = person.Xp;
                long currentGain = person.BekommtZurzeitSoVielXp;
                if (person.ZuletztImChannel.AddMinutes(1) < DateTime.Now)
                {
                    currentGain = 0;
                }

                long platz = repository.HolePlatzDesUsersBeiXp(guildUser.Id);

                int level = 1;
                int xpForNextLevel = startXp;
                long restXp = xp;

                while (restXp >= xpForNextLevel)
                {
                    restXp -= xpForNextLevel;
                    level++;
                    xpForNextLevel = (int)Math.Round(xpForNextLevel * faktor);
                }

                long xpToNextLevel = xpForNextLevel - restXp;

                var embed = new EmbedBuilder()
                    .WithTitle("Dein Level-Fortschritt")
                    .WithColor(Color.DarkRed)
                    //.WithImageUrl(command.User.GetAvatarUrl())
                    .AddField("Level", $"```{level}```", true)
                    .AddField("XP", $"```{xp}```", true)
                    .AddField($"XP bis Level {level + 1}", $"```{xpToNextLevel}```")
                    .AddField("Dein Platz in Vergleich zu allen", $"```{platz}```")
                    .AddField($"Du bekommst zurzeit", $"```{currentGain} XP / MIN```")
                    .Build();

                await command.FollowupAsync(embed: embed, ephemeral: true);
            }
        }
    }

    private static Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }
}