using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using UtilsBot.Domain.Contracts;
using UtilsBot.Domain.Models;

namespace UtilsBot.Services;

public class DiscordService : IHostedService
{
    private const string InterestedCommand = "interested";
    private const string InfoCommand = "info";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordSocketClient _client;
    private readonly string _token;
    private readonly BotConfig _config;
    private readonly VoiceChannelChangeListenerService _listener;

    public DiscordService(IOptions<BotConfig> config, IOptions<Secrets> secrets, IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _token = secrets.Value.DiscordToken;
        _config = config.Value;
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        });
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _listener = new VoiceChannelChangeListenerService(scopeFactory, config.Value);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _listener.Dispose();
        await _client.LogoutAsync();
        await _client.StopAsync();
        await _client.DisposeAsync();
    }

    private async Task ReadyAsync()
    {
        Console.WriteLine($"Online as: {_client.CurrentUser}");

        _client.MessageReceived += message =>
        {
            if (message.Author.IsBot) return Task.CompletedTask;

            Console.WriteLine($"Nachricht von {message.Author.Username}: {message.Content}");
            return Task.CompletedTask;
        };

        _client.SlashCommandExecuted += SlashCommandHandlerAsync;

        await RegistriereCommands(_client);

        await _listener.StartPeriodicCheck(_client);
    }

    private async Task RegistriereCommands(DiscordSocketClient client)
    {
        foreach (var guildId in client.Guilds.Select(g => g.Id))
        {
            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName(InterestedCommand)
                .WithDescription("Bekomme Benachrichtigungen wenn Personen einen Discord Channel beitreten")
                .AddOption("von", ApplicationCommandOptionType.Integer, "Von (z.B Wert:15 für ab 15 Uhr Nachmittags)",
                    true)
                .AddOption("bis", ApplicationCommandOptionType.Integer, "Bis (z.B Wert:23 für bis 23 Uhr Abends)", true)
                .Build(), guildId);

            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName(InfoCommand)
                .WithDescription("Auskunft über deinen Fortschritt")
                .Build(), guildId);
        }
    }

    private async Task SlashCommandHandlerAsync(SocketSlashCommand command)
    {
        if (!_config.KommandosAktiviert)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var discordCommandHandler = scope.ServiceProvider.GetRequiredService<IDiscordCommandHandler>();
        switch (command.CommandName)
        {
            case InterestedCommand:
                await discordCommandHandler.InterestedAsync(command);
                break;
            case InfoCommand:
                await discordCommandHandler.InfoAsync(command);
                break;
            default:
                throw new ArgumentException($"Command {command.CommandName} not implemented");
        }
    }

    private static Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }
}