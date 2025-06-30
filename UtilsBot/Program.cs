using UtilsBot.Repository;
using UtilsBot.Services;
using Timer = System.Timers.Timer;
using Microsoft.Extensions.Configuration;
public class Program
{
    private DiscordService _discordService;

    public Program(DiscordService discordService)
    {
        _discordService = discordService;
       
    }

    public static Task Main(string[] args)
    {
        var token = Environment.GetEnvironmentVariable("DiscordToken");
        if (token == null)
        {
            throw new Exception("Discord token not found \n SET WITH -> setx DiscordToken 'tokenValue'");
        }
        
        return new Program(new DiscordService(new VoiceChannelChangeListenerService(new DatabaseRepository(), new Timer()), token))
            .MainAsync();
    }

    public async Task MainAsync()
    {
        await _discordService.StartWorking();
    }
}