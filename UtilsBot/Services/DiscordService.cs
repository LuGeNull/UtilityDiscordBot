using System.Net.Mime;
using Discord;
using Discord.WebSocket;
using UtilsBot.Datenbank;
using UtilsBot.Domain;
using UtilsBot.Repository;

namespace UtilsBot.Services;

public class DiscordService
{
    private readonly DiscordSocketClient _client;
    private readonly LevelService _levelService;
    private readonly string _token;
    private readonly DiscordServerChangeMonitor _discordServerChangeListener;
    private readonly CommandRegistrationService _commandRegistrationService;
    private readonly EmbedFactory _embedFactory;
    private readonly BetService _betService;
    private readonly EventHandlerService _eventHandlerService;

    public DiscordService(string token)
    {
        _token = token;
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        });
        _client.Log += LogAsync;
        _levelService = new LevelService();
        _betService = new BetService();
        _discordServerChangeListener = new DiscordServerChangeMonitor();
        _commandRegistrationService = new CommandRegistrationService(_client);
        _embedFactory = new EmbedFactory(new CalculatorService());
        _eventHandlerService = new EventHandlerService(
            _client,
            _betService,
            _levelService,
            _embedFactory,
            _commandRegistrationService);

        _client.Ready += ReadyAsync;
    }

    public async Task StartWorking()
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private async Task ReadyAsync()
    {
        Console.WriteLine($"Online as: {_client.CurrentUser}");
        
        _eventHandlerService.RegisterEventHandlers();

        // Kommandos registrieren und Server-Änderungen überwachen
        _eventHandlerService.RegisterCommands();
        await _discordServerChangeListener.StartPeriodicCheck(_client);
    }


    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }
}