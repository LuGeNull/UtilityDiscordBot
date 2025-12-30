using Microsoft.EntityFrameworkCore;
using UtilsBot.Services;
using UtilsBot.Datenbank;
using UtilsBot.Domain;
using DotNetEnv;

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
        return new Program(new DiscordService(ApplicationState.Token))
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
        Env.Load();

        ApplicationState.TestToken = Env.GetString("DiscordTokenTest")
                                     ?? Environment.GetEnvironmentVariable("DiscordTokenTest");

        ApplicationState.ProdToken = Env.GetString("DiscordToken")
                                     ?? Environment.GetEnvironmentVariable("DiscordToken");
        if (ApplicationState.TestToken == null && ApplicationState.ProdToken == null)
        {
            throw new Exception(".env is missing or contains the wrong value \n Create the .env in the Folder of the Executable");
        }
        
        if (ApplicationState.ProdToken == null)
        {
            ApplicationState.TestMode = true;
            return;
        }
        
        if (ApplicationState.TestToken == null)
        {
            ApplicationState.TestMode = false;
            return;
        }
        Console.Write($"RUN Mode: {ApplicationState.TestMode}");
    }

    public async Task MainAsync()
    {
        await _discordService.StartWorking();
    }
}