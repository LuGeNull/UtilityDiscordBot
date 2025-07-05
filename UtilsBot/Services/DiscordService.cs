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
        _client.SlashCommandExecuted += SlashCommandHandlerAsync;

        RegistriereCommands(_client);
        _voiceChannelChangeListener.StartPeriodicCheck(_client);
        
        
        return Task.CompletedTask;
    }

    private async void RegistriereCommands(DiscordSocketClient client)
    {
        foreach (var guildId in client.Guilds.Select(g => g.Id))
        {
            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("interested")
                .WithDescription("Bekomme Benachrichtigungen wenn Personen einen Discord Channel beitreten")
                .AddOption("von", ApplicationCommandOptionType.Integer, "Von (z.B Wert:15 für ab 15 Uhr Nachmittags)", isRequired: true)
                .AddOption("bis", ApplicationCommandOptionType.Integer, "Bis (z.B Wert:23 für bis 23 Uhr Abends)", isRequired: true)
                .Build(), guildId);
            
            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("xp")
                .WithDescription("Auskunft über deine derzeitige XP")
                .Build(), guildId);
        }
    }

    private async Task SlashCommandHandlerAsync(SocketSlashCommand command)
    {
        if (command.CommandName == "interested")
        {
            var von = (long?)command.Data.Options.FirstOrDefault(x => x.Name == "von")?.Value ?? 0L;
            var bis = (long?)command.Data.Options.FirstOrDefault(x => x.Name == "bis")?.Value ?? 0L;

            if (command.User is SocketGuildUser guildUser)
            {
                _voiceChannelChangeListener.AddUserToInterestedPeopleList(
                    guildUser.Id, guildUser.DisplayName, guildUser.Guild.Id, von, bis);
                await command.RespondAsync("I'll notify you!");
            }
        }
        
        if (command.CommandName == "xp")
        {
            if (command.User is SocketGuildUser guildUser)
            {
                var xp = _voiceChannelChangeListener._database.HoleUserXpMitId(guildUser.Id);
                await command.RespondAsync($"Du hast zurzeit {xp} Xp");
            }
        }
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }
    
}