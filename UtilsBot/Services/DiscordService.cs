namespace UtilsBot.Services;
using Discord;
using Discord.WebSocket;

public class DiscordService
{
    private DiscordSocketClient _client;
    private VoiceChannelChangeListenerService _voiceChannelChangeListener;
    private string _token;
    public DiscordService(VoiceChannelChangeListenerService voiceChannelChangeListener, string token)
    {
        _token = token;
        _voiceChannelChangeListener = voiceChannelChangeListener;
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        });
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += MessageReceivedAsync;
    }
    
    public async Task StartWorking()
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();

        await Task.Delay(-1);
    }
    
    private Task ReadyAsync()
    {
        Console.WriteLine($"Onlien as: {_client.CurrentUser}");
        _voiceChannelChangeListener.StartPeriodicCheck(_client);
        
        
        return Task.CompletedTask;
    }
    
    private async Task MessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.Id == _client.CurrentUser.Id)
            return;

        if (message.Content.StartsWith("!interested"))
        {
            if (message.Author is SocketGuildUser guildUser)
            {
                _voiceChannelChangeListener.AddUserToInterestedPeopleList(guildUser.Id,guildUser.Guild.Id);
                var resultMessage = await message.Channel.SendMessageAsync("I'll notify you!");
            }
        }
    }
    
    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }
    
}