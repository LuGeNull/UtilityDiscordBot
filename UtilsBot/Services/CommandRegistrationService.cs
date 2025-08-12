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
                .WithName("xp")
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
                .WithName("betstart")
                .WithDescription("Wettestarten")
                .AddOption("titel", ApplicationCommandOptionType.String, "Titel der Wette", isRequired: true)
                .AddOption("ereignis1", ApplicationCommandOptionType.String, "Name Ereignis A z.B Spanien gewinnt",
                    isRequired: true)
                .AddOption("ereignis2", ApplicationCommandOptionType.String, "Name Ereignis B z.B Deutschland gewinnt",
                    isRequired: true)
                .AddOption("annahmeschluss", ApplicationCommandOptionType.Integer, "Annahmeende in Stunden ab jetzt",
                    isRequired: true)
                .AddOption("maxpayoutmultiplikator", ApplicationCommandOptionType.Integer,
                    "Was kann jemand höchstens gewinnen von seinem Einsatz - Standard = 3 für 3X Max Payout",
                    isRequired: false)
                .Build(), guildId);
        }
    }
    
}

