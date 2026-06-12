using Discord;
using Discord.WebSocket;

namespace UtilsBot.Services;

public class CommandRegistrationService 
{
    private readonly DiscordSocketClient _client;

    public CommandRegistrationService(DiscordSocketClient client)
    {
        _client = client;
    }

    public async Task RegisterCommands()
    {
        foreach (var guildId in _client.Guilds.Select(g => g.Id))
        {
            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("info")
                .WithDescription("Auskunft über deinen Fortschritt")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("transparenz")
                    .WithDescription("Wähle die Transparenz")
                    .WithType(ApplicationCommandOptionType.String)
                    .AddChoice("Transparent", "transparent")
                    .AddChoice("Nicht Transparent", "not_transparent")
                    .WithRequired(false))
                .Build(), guildId);

            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("leaderboardxp")
                .WithDescription("Auskunft über die XP der Top 8")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("transparenz")
                    .WithDescription("Wähle die Transparenz")
                    .WithType(ApplicationCommandOptionType.String)
                    .AddChoice("Transparent", "transparent")
                    .AddChoice("Nicht Transparent", "not_transparent")
                    .WithRequired(false))
                .Build(), guildId);

            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("warerasubscribe")
                .WithDescription("Subscribe this channel for WarEra contract notifications")
                .Build(), guildId);

            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("startsearch")
                .WithDescription("Start WarEra contract notifications for this guild")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("minrate")
                    .WithDescription("Minimum currentPerK rate to notify on (e.g. 0.1). Omit to keep current value.")
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(false))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("maxdamage")
                    .WithDescription("Max damage you can do; contracts with higher min damage are skipped (e.g. 500000)")
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(false))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("pro")
                    .WithDescription("Include professionalsOnly contracts. Omit to keep current value.")
                    .WithType(ApplicationCommandOptionType.Boolean)
                    .WithRequired(false))
                .Build(), guildId);

            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("endsearch")
                .WithDescription("Stop WarEra contract notifications for this guild")
                .Build(), guildId);

            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("warerasetapikey")
                .WithDescription("Set the WarEra API key")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("apikey")
                    .WithDescription("The API key to use for WarEra")
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(true))
                .Build(), guildId);

            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("warerastatus")
                .WithDescription("Check the status of WarEra contract monitoring for this guild")
                .Build(), guildId);

            await _client.Rest.CreateGuildCommand(new SlashCommandBuilder()
                .WithName("wareratestnotify")
                .WithDescription("Send a test notification using the latest active contract (bypasses dedupe).")
                .Build(), guildId);
        }
    }
    
}

