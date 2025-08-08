using Microsoft.EntityFrameworkCore;
using UtilsBot.Repository;
using UtilsBot.Services;
using Timer = System.Timers.Timer;
using Microsoft.Extensions.Configuration;
using UtilsBot;
using UtilsBot.Datenbank;
using UtilsBot.Domain;

public class Program
{
    private DiscordService _discordService;

    public Program(DiscordService discordService)
    {
        _discordService = discordService;
    }

    public static Task Main(string[] args)
    {
        DatabaseMigration();
        UeberpruefeBotToken();
        return new Program(new DiscordService(new DiscordServerChangeMonitor(), ApplicationState.Token, new BetService()))
            .MainAsync();
    }

    private static void DatabaseMigration()
    {
        using (var context = new BotDbContext())
        {
            context.Database.Migrate();
        }
    }

    private static void UeberpruefeBotToken()
    {
        ApplicationState.TestToken = Environment.GetEnvironmentVariable("DiscordTokenTest") ?? "";
        ApplicationState.ProdToken = Environment.GetEnvironmentVariable("DiscordToken") ?? "";
        
        if (string.IsNullOrEmpty(ApplicationState.TestToken) || string.IsNullOrEmpty(ApplicationState.ProdToken))
        {
            if (string.IsNullOrEmpty(ApplicationState.TestToken))
            {
                ApplicationState.TestMode = false;
            }
            if (string.IsNullOrEmpty(ApplicationState.ProdToken))
            {
                ApplicationState.TestMode = true;
            }

            if (string.IsNullOrEmpty(ApplicationState.TestToken) && string.IsNullOrEmpty(ApplicationState.ProdToken))
            {
                throw new Exception("Discord token not found \n SET WITH -> setx DiscordToken 'tokenValue'");
            }
        }
        else
        {
            ApplicationState.TestMode = true;
        }
    }

    public async Task MainAsync()
    {
        await _discordService.StartWorking();
    }
}