using System.Net.Mime;
using Discord;
using Discord.WebSocket;
using UtilsBot.Request;
using UtilsBot.Response;

namespace UtilsBot.Services;

public class DiscordService
{
    private readonly DiscordSocketClient _client;
    private readonly LevelService _levelService;
    private readonly string _token;
    private readonly DiscordServerChangeMonitor _discordServerChangeListener;

    public DiscordService(DiscordServerChangeMonitor discordServerChangeListener, string token)
    {
        _levelService = new LevelService();
        _token = token;
        _discordServerChangeListener = discordServerChangeListener;
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        });
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
    }

    public async Task StartWorking()
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private async Task ReadyAsync()
    {
        Console.WriteLine($"Online as: {_client.CurrentUser}");
        
        _client.MessageReceived += async (message) =>
        {
            if (message.Author.IsBot) return;
            await _levelService.HandleRequest(new MessageSentRequest(message.Author.Id, message));
            Console.WriteLine($"Nachricht von {message.Author.Username}: {message.Content}");
        };
        
        _client.SlashCommandExecuted += SlashCommandHandlerAsync;

        RegistriereCommands(_client);
        await _discordServerChangeListener.StartPeriodicCheck(_client);
        
    }

    private async void RegistriereCommands(DiscordSocketClient client)
    {
        foreach (var guildId in client.Guilds.Select(g => g.Id))
        {
            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("xp")
                .WithDescription("Auskunft über deinen Fortschritt")
                .Build(), guildId);
            
            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("xptransparent")
                .WithDescription("Auskunft über deinen Fortschritt für alle einsehbar")
                .Build(), guildId);
            
            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("leaderboardxp")
                .WithDescription("Auskunft über die XP der Top 8")
                .Build(), guildId);
            
            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("leaderboardxptransparent")
                .WithDescription("Auskunft über die XP der Top 8 für alle einsehbar")
                .Build(), guildId);
        }
    }

    private async Task SlashCommandHandlerAsync(SocketSlashCommand command)
    {
        if (command.CommandName == "xp")
        {
            var ephimeral = true;
            await XpResponse(command, ephimeral);
        }
        
        if (command.CommandName == "xptransparent")
        {
            var ephimeral = false;
            await XpResponse(command, ephimeral);
        }
        
        if (command.CommandName == "leaderboardxp")
        {
            var invisibleMessage = true;
            await LeaderboardXpResponse(command, invisibleMessage);
        }
        
        if (command.CommandName == "leaderboardxptransparent")
        {
            var invisibleMessage = false;
            await LeaderboardXpResponse(command, invisibleMessage);
        }
    }

    private async Task LeaderboardXpResponse(SocketSlashCommand command, bool invisibleMessage)
    {
        if (command.User is SocketGuildUser guildUser)
        {
            await command.DeferAsync(ephemeral: invisibleMessage);
            var leaderboardResponse = await _levelService.HandleRequest(new XpLeaderboardRequest(guildUser.Guild.Id));
                
            var embedBuilder = new EmbedBuilder()
                .WithTitle("XP Leaderboard")
                .WithColor(Color.DarkRed);

            for (int i = 0; i < leaderboardResponse.personen.Count; i++)
            {
                embedBuilder
                    .AddField($"Platz {i + 1}:", $"```{leaderboardResponse.personen[i].DisplayName}```", true)
                    .AddField("Level", $"```{_levelService.BerechneLevelUndRestXp(leaderboardResponse.personen[i].Xp)}```", true)
                    .AddField("XP", $"```{leaderboardResponse.personen[i].Xp}```", true);
            }

            var embed = embedBuilder.Build();
               
                
            var followupMessage = await command.FollowupAsync(embed: embed, ephemeral: invisibleMessage);
            if (!invisibleMessage)
            {
                NachrichtenLoeschenNachXMinuten(followupMessage);
            }
        }
    }

    private async Task XpResponse(SocketSlashCommand command, bool invisibleMessage)
    {
        if (command.User is SocketGuildUser guildUser)
        {
            await command.DeferAsync(ephemeral: invisibleMessage);
            var xpResponse = await _levelService.HandleRequest(new XpRequest(guildUser.Id, guildUser.DisplayName, guildUser.Guild.Id));
                
            var embed = XpEmbed(xpResponse);
                
            var followupMessage = await command.FollowupAsync(embed: embed, ephemeral: invisibleMessage);
            if (!invisibleMessage)
            {
                NachrichtenLoeschenNachXMinuten(followupMessage);
            }
        }
    }

    private static Embed XpEmbed(XpResponse xpResponse)
    {
        return new EmbedBuilder()
            .WithTitle("Dein Level-Fortschritt")
            .WithColor(Color.DarkRed)
            .AddField("Level",$"```{xpResponse.level}```", true)
            .AddField("XP",$"```{xpResponse.xp}```",true)
            .AddField($"XP bis Level {xpResponse.level+1}" , $"```{xpResponse.xpToNextLevel}```")
            .AddField("Dein Platz in Vergleich zu allen",$"```{xpResponse.platzDerPerson}```")
            .AddField($"Du bekommst zurzeit", $"```{xpResponse.currentGain} XP / MIN```")
            .AddField($"Nachrichtenpunkte heute verdient", $"```{xpResponse.nachrichtenPunkte} XP / {ApplicationState.NachrichtenpunkteTaeglich} XP```")
            .Build();
    }

    private static void NachrichtenLoeschenNachXMinuten(IUserMessage sendTask)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(ApplicationState.NachrichtenWerdenGeloeschtNachXMinuten));
            await sendTask.Channel.DeleteMessageAsync(sendTask.Id);
        });
    }
    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }
}