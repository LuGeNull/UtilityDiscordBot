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
        }
    }
    
}

