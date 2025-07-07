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
        
        ApplicationState.TestMode = true;
        ApplicationState.KommandosAktiviert = true;
        var token = "";
        if (ApplicationState.TestMode)
        {
            token = Environment.GetEnvironmentVariable("DiscordTokenTest");
            ApplicationState.NachrichtenWerdenGeloeschtNachXMinuten = 1;
            ApplicationState.TickProXSekunden = 60000;
            ApplicationState.BaseXp = 4;
            ApplicationState.UserXMinutenAusDemChannel = 1;
            ApplicationState.StreamOrVideoBonus = 2;
            ApplicationState.VideoOnlyBonus = 2; 
            ApplicationState.StreamAndVideoBonus = 4; 
            ApplicationState.FullMuteBaseXp = 2;
            ApplicationState.OnlyMuteBaseXp = 3;
            ApplicationState.NachrichtenVerschicken = false;
        }
        else
        {
            token = Environment.GetEnvironmentVariable("DiscordToken");
            ApplicationState.NachrichtenWerdenGeloeschtNachXMinuten = 30;
            ApplicationState.TickProXSekunden = 60000;
            ApplicationState.BaseXp = 4;
            ApplicationState.UserXMinutenAusDemChannel = 30;
            ApplicationState.StreamOrVideoBonus = 2;
            ApplicationState.VideoOnlyBonus = 2; 
            ApplicationState.StreamAndVideoBonus = 4; 
            ApplicationState.FullMuteBaseXp = 2;
            ApplicationState.OnlyMuteBaseXp = 3;
            ApplicationState.NachrichtenVerschicken = true;
        }
        
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