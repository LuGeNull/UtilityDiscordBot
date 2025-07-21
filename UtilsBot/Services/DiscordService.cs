using System.Net.Mime;
using Discord;
using Discord.WebSocket;
using UtilsBot.Request;

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

    private Task ReadyAsync()
    {
        Console.WriteLine($"Online as: {_client.CurrentUser}");
        
        _client.MessageReceived += async (message) =>
        {
            if (message.Author.IsBot) return;
            _levelService.HandleRequest(new MessageSentRequest(message.Author.Id, message));
            Console.WriteLine($"Nachricht von {message.Author.Username}: {message.Content}");
        };
        
        _client.SlashCommandExecuted += SlashCommandHandlerAsync;

        RegistriereCommands(_client);
        _discordServerChangeListener.StartPeriodicCheck(_client);
        
        return Task.CompletedTask;
    }

    private async void RegistriereCommands(DiscordSocketClient client)
    {
        foreach (var guildId in client.Guilds.Select(g => g.Id))
        {
            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("xp")
                .WithDescription("Auskunft über deinen Fortschritt")
                .Build(), guildId);
        }
    }

    private async Task SlashCommandHandlerAsync(SocketSlashCommand command)
    {
        if (command.CommandName == "xp")
        {
            if (!ApplicationState.KommandosAktiviert)
            {
                return;
            }
            
            if (command.User is SocketGuildUser guildUser)
            {
                await command.DeferAsync(ephemeral: true);
                var xpResponse = _levelService.HandleRequest(new XpRequest(guildUser.Id, guildUser.DisplayName, guildUser.Guild.Id));
                
                var embed = new EmbedBuilder()
                    .WithTitle("Dein Level-Fortschritt")
                    .WithColor(Color.DarkRed)
                    .AddField("Level",$"```{xpResponse.level}```", true)
                    .AddField("XP",$"```{xpResponse.xp}```",true)
                    .AddField($"XP bis Level {xpResponse.level+1}" , $"```{xpResponse.xpToNextLevel}```")
                    .AddField("Dein Platz in Vergleich zu allen",$"```{xpResponse.platzDerPerson}```")
                    .AddField($"Du bekommst zurzeit", $"```{xpResponse.currentGain} XP / MIN```")
                    .Build();
                
                await command.FollowupAsync(embed: embed, ephemeral: true);
            } 
        }
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }
}