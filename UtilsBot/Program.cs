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
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        if (config["Discord:BotToken"] == null)
        {
            Console.WriteLine("Discord Bot Token is required.");
        }
        
        return new Program(new DiscordService(new VoiceChannelChangeListenerService(new DatabaseRepository(), new Timer()), config["Discord:BotToken"]))
            .MainAsync();
    }

    public async Task MainAsync()
    {
        await _discordService.StartWorking();
    }
}