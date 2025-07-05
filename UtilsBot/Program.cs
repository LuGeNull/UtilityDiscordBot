using UtilsBot.Repository;
using UtilsBot.Services;
using Timer = System.Timers.Timer;
using Microsoft.Extensions.Configuration;
using UtilsBot;

public class Program
{
    private DiscordService _discordService;

    public Program(DiscordService discordService)
    {
        _discordService = discordService;
       
    }

    public static Task Main(string[] args)
    {
        ApplicationState.TestMode = false;
        if (ApplicationState.TestMode)
        {
            ApplicationState.NachrichtenWerdenGeloeschtNachXMinuten = 1;
            ApplicationState.TickProXSekunden = 60000;
            ApplicationState.BaseXp = 4;
            ApplicationState.UserXMinutenAusDemChannel = 1;
        }
        else
        {
            ApplicationState.NachrichtenWerdenGeloeschtNachXMinuten = 30;
            ApplicationState.TickProXSekunden = 60000;
            ApplicationState.BaseXp = 4;
            ApplicationState.UserXMinutenAusDemChannel = 30;
        }
       
        
        var token = Environment.GetEnvironmentVariable("DiscordToken");
        if (token == null)
        {
            throw new Exception("Discord token not found \n SET WITH -> setx DiscordToken 'tokenValue'");
        }
        
        return new Program(new DiscordService(new VoiceChannelChangeListenerService(new DatabaseRepository()), token))
            .MainAsync();
    }

    public async Task MainAsync()
    {
        await _discordService.StartWorking();
    }
}