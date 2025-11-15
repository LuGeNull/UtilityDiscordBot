using Discord;
using UtilsBot.Domain;
using UtilsBot.Domain.Xp;

namespace UtilsBot.Services;

public class EmbedFactory
{
    public async Task<Embed> BuildInfoEmbed(InfoResponse infoResponse)
    {
        return new EmbedBuilder()
            .WithTitle("Your Progress")
            .WithColor(Color.DarkRed)
            .AddField("Level", $"```{infoResponse.level}```", true)
            .AddField("XP", $"```{infoResponse.xp}```", true)
            .AddField($"XP until Level {infoResponse.level + 1}", $"```{infoResponse.xpToNextLevel}```")
            .AddField("Your place in comparison to others", $"```{infoResponse.platzDerPerson}```")
            .AddField($"You're currently gaining", $"```{infoResponse.currentXpGain} XP / MIN ```")
            .AddField($"XP earned by Messages today",
                $"```{infoResponse.nachrichtenPunkte} XP / {ApplicationState.MessagePointsDaily} XP```")
            .Build();
    }
}