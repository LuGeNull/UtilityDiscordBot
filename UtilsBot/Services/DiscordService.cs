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
        Console.WriteLine($"Online as: {_client.CurrentUser}");
        _voiceChannelChangeListener.StartPeriodicCheck(_client);
        
        
        return Task.CompletedTask;
    }
    
    private async Task MessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.Id == _client.CurrentUser.Id)
            return;

        if (message.Content.StartsWith("!interested"))
        {
            
            var NichtBenachrichtigungszeitraum = message.Content.Split(' ');
            var nichtbenachrichtigungsZeitraumVon = 0;
            var nichtbenachrichtigungsZeitraumBis = 0;
            var IstEingabeErfolgreichGewesen = UeberpruefeEingabe(message, NichtBenachrichtigungszeitraum, out nichtbenachrichtigungsZeitraumVon, out nichtbenachrichtigungsZeitraumBis);
            
            
            if (message.Author is SocketGuildUser guildUser && IstEingabeErfolgreichGewesen)
            {
                _voiceChannelChangeListener.AddUserToInterestedPeopleList(guildUser.Id, guildUser.DisplayName,guildUser.Guild.Id, nichtbenachrichtigungsZeitraumVon, nichtbenachrichtigungsZeitraumBis);
                var resultMessage = await message.Channel.SendMessageAsync("I'll notify you!");
            }
        }
    }

    private static bool UeberpruefeEingabe(SocketMessage message, string[] NichtBenachrichtigungszeitraum,
        out int nichtbenachrichtigungsZeitraumVon, out int nichtbenachrichtigungsZeitraumBis)
    {
        if (NichtBenachrichtigungszeitraum.Length > 2)
        {
            try
            {
                nichtbenachrichtigungsZeitraumVon = int.Parse(NichtBenachrichtigungszeitraum[1]);
                nichtbenachrichtigungsZeitraumBis = int.Parse(NichtBenachrichtigungszeitraum[2]);
                if (nichtbenachrichtigungsZeitraumVon < 0 || nichtbenachrichtigungsZeitraumVon > 24)
                { 
                     SendeNachrichtBeiFehlerhafterEingabe(message);
                     return false;
                }
                if (nichtbenachrichtigungsZeitraumBis < 0 || nichtbenachrichtigungsZeitraumBis > 24)
                {
                    SendeNachrichtBeiFehlerhafterEingabe(message);
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                SendeNachrichtBeiFehlerhafterEingabe(message);
                    
            }
        }
        else
        {
            nichtbenachrichtigungsZeitraumVon = 0;
            nichtbenachrichtigungsZeitraumBis = 24;
            return true;
        }
        nichtbenachrichtigungsZeitraumVon = 0;
        nichtbenachrichtigungsZeitraumBis = 24;
        return false;
    }

    private static async Task SendeNachrichtBeiFehlerhafterEingabe(SocketMessage message)
    {
        await message.Channel.SendMessageAsync("Das war nicht richtig minjung -> !Interested 7 14 <- Informiert dich zwischen 14.01 bis 6:59");
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }
    
}