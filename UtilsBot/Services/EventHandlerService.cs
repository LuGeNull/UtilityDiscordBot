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
    private readonly MessageService _messageService;
    private readonly RoleService _roleService;

    public EventHandlerService(
        DiscordSocketClient client,
        LevelService levelService,
        EmbedFactory embedFactory,
        CommandRegistrationService commandRegistrationService)
    {
        _client = client;
        _levelService = levelService;
        _embedFactory = embedFactory;
        _commandRegistrationService = commandRegistrationService;
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
        try
        {
            if (message.Author.IsBot) return;
            await using var db = new DatabaseRepository(new BotDbContext());
            await _messageService.HandleRequest(new MessageSentRequest(message.Author.Id, message), db);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EventHandlerService] Fehler bei HandleMessageReceived: {ex}");
        }
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

    private async Task SlashCommandHandlerAsync(SocketSlashCommand command)
    {
        try
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

            if (command.CommandName == "rebuild-database")
            {
                await RebuildDatabaseResponse(command, db);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EventHandlerService] Fehler bei Slash-Command '{command.CommandName}': {ex}");
            await BenachrichtigeUeberFehlerAsync(command);
        }
    }

    private static async Task BenachrichtigeUeberFehlerAsync(SocketSlashCommand command)
    {
        try
        {
            await command.RespondAsync("Es ist ein Fehler aufgetreten.", ephemeral: true);
        }
        catch
        {
            try
            {
                await command.FollowupAsync("Es ist ein Fehler aufgetreten.", ephemeral: true);
            }
            catch
            {
                // Nutzer konnte nicht benachrichtigt werden - kein weiterer Handlungsbedarf
            }
        }
    }

    private async Task RebuildDatabaseResponse(SocketSlashCommand command, DatabaseRepository db)
    {
        if (command.User is not SocketGuildUser guildUser)
        {
            return;
        }

        if (!guildUser.GuildPermissions.Administrator)
        {
            await command.RespondAsync("Dafür brauchst du Administrator-Rechte.", ephemeral: true);
            return;
        }

        await command.DeferAsync(ephemeral: true);

        var guild = guildUser.Guild;
        await guild.DownloadUsersAsync();

        int erstellt = 0;
        int aktualisiert = 0;
        int uebersprungen = 0;
        int fehler = 0;

        foreach (var member in guild.Users)
        {
            if (member.IsBot) continue;

            try
            {
                var hoechstesLevel = _roleService.ErmittleHoechstesLevelAusRollen(member);

                if (hoechstesLevel == 0)
                {
                    uebersprungen++;
                    continue;
                }

                var minimumXp = _levelService.BerechneMinimumXpFuerLevel(hoechstesLevel);
                var existierteVorher = await db.UpsertUserWithXpAsync(member.Id, member.DisplayName, guild.Id, minimumXp);

                if (existierteVorher) aktualisiert++;
                else erstellt++;
            }
            catch (Exception ex)
            {
                fehler++;
                Console.WriteLine($"[EventHandlerService] Fehler beim Wiederherstellen von Nutzer {member.Id}: {ex}");
            }
        }

        var fehlerHinweis = fehler > 0 ? $", {fehler} mit Fehler" : "";
        await command.FollowupAsync(
            $"Datenbank wiederhergestellt: {erstellt} neu angelegt, {aktualisiert} aktualisiert, {uebersprungen} ohne Level-Rolle übersprungen{fehlerHinweis}.",
            ephemeral: true);
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
