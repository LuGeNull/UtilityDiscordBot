using Discord;
using UtilsBot.Domain;
using UtilsBot.Domain.Xp;

namespace UtilsBot.Services;

public class EmbedFactory
{
    public async Task<Embed> BuildInfoEmbed(InfoResponse infoResponse)
    {
        return new EmbedBuilder()
            .WithTitle("Dein Fortschritt")
            .WithColor(Color.DarkRed)
            .AddField("Level", $"```{infoResponse.level}```", true)
            .AddField("XP", $"```{infoResponse.xp}```", true)
            .AddField($"XP bis Level {infoResponse.level + 1}", $"```{infoResponse.xpToNextLevel}```")
            .AddField("Dein Platz im Vergleich zu allen", $"```{infoResponse.platzDerPerson}```")
            .AddField($"Du bekommst zurzeit", $"```{infoResponse.currentXpGain} XP / MIN | {infoResponse.currentGoldGain.ToString().Replace(',', '.')} GOLD / MIN```")
            .AddField($"Nachrichtenpunkte heute verdient",
                $"```{infoResponse.nachrichtenPunkte} XP / {ApplicationState.MessagePointsDaily} XP```")
            .Build();
    }
}