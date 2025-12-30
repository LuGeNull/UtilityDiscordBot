using Discord;
using Discord.WebSocket;
using UtilsBot.Datenbank;
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
