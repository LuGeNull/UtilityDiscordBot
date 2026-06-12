using Microsoft.EntityFrameworkCore;
using UtilsBot.Services;
using UtilsBot.Datenbank;
using UtilsBot.Domain;
using UtilsBot.Repository;
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
        LoadSettingsFromDatabase().Wait();
        return new Program(new DiscordService(ApplicationState.Token))
            .MainAsync();
    }

    private static async Task LoadSettingsFromDatabase()
    {
        await using (var db = new BotDbContext())
        {
            var repository = new DatabaseRepository(db);
            var apiKey = await repository.GetSettingAsync("WarEraApiKey");
            if (!string.IsNullOrEmpty(apiKey))
            {
                ApplicationState.WarEraApiKey = apiKey;
            }
        }
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

        ApplicationState.WarEraApiKey = Env.GetString("WarEraApiKey")
                                        ?? Environment.GetEnvironmentVariable("WarEraApiKey");

        var channelIdStr = Env.GetString("WarEraNotificationChannelId")
                           ?? Environment.GetEnvironmentVariable("WarEraNotificationChannelId");
        if (ulong.TryParse(channelIdStr, out var channelId))
        {
            ApplicationState.WarEraNotificationChannelId = channelId;
        }

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