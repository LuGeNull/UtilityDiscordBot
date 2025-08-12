using Discord;
using UtilsBot.Domain;
using UtilsBot.Domain.Xp;

namespace UtilsBot.Services;

public class EmbedFactory
{
    private readonly CalculatorService _calculatorService;
    public EmbedFactory(CalculatorService calculatorService)
    {
        _calculatorService = calculatorService;
    }

    public async Task<Embed> BuildXpEmbed(XpResponse xpResponse)
    {
        return new EmbedBuilder()
            .WithTitle("Dein Level-Fortschritt")
            .WithColor(Color.DarkRed)
            .AddField("Level", $"```{xpResponse.level}```", true)
            .AddField("XP", $"```{xpResponse.xp}```", true)
            .AddField($"XP bis Level {xpResponse.level + 1}", $"```{xpResponse.xpToNextLevel}```")
            .AddField("Dein Platz in Vergleich zu allen", $"```{xpResponse.platzDerPerson}```")
            .AddField($"Du bekommst zurzeit", $"```{xpResponse.currentGain} XP / MIN```")
            .AddField($"Nachrichtenpunkte heute verdient",
                $"```{xpResponse.nachrichtenPunkte} XP / {ApplicationState.NachrichtenpunkteTaeglich} XP```")
            .Build();
    }
    
    public async Task<Embed> BuildBetEmbed(string betTitle, string siteAName, List<(string user, int betAmount)> siteA,
        string siteBName, List<(string user, int betAmount)> siteB, long endOfBetInHours,
        bool betIsBeingCanceled = false, long maxPayoutMultiplicator = 3)
    {
         var embedBuilder = new EmbedBuilder()
            .WithTitle($"{betTitle}")
            .WithColor(Color.DarkBlue);

        string usersA = "";
        string usersB = "";

        if (betIsBeingCanceled)
        {
            usersA = siteA.Count == 0
                ? "⚠️ Kein Teilnehmer hat gesetzt"
                : string.Join("\n", siteA.Select(x => $"{x.user}: {x.betAmount} XP (zurückerstattet)"));
            usersB = siteB.Count == 0
                ? "⚠️ Kein Teilnehmer hat gesetzt"
                : string.Join("\n", siteB.Select(x => $"{x.user}: {x.betAmount} XP (zurückerstattet)"));
        }
        else
        {
            usersA = siteA.Count == 0
                ? "Noch keine Teilnehmer"
                : string.Join("\n", siteA.Select(x => $"{x.user}: {x.betAmount} XP"));
            usersB = siteB.Count == 0
                ? "Noch keine Teilnehmer"
                : string.Join("\n", siteB.Select(x => $"{x.user}: {x.betAmount} XP"));
        }

        string seiteAContent = $"```{usersA}```";
        string seiteBContent = $"```{usersB}```";

        var closingDate = DateTime.UtcNow.AddHours(endOfBetInHours);
        var unixTimestamp = ((DateTimeOffset)closingDate).ToUnixTimeSeconds();

        if (betIsBeingCanceled)
        {
            var cancelDate = DateTime.UtcNow;
            var unixTimestampCancelDate = ((DateTimeOffset)cancelDate).ToUnixTimeSeconds();

            embedBuilder
                .AddField($"{siteAName} (Option A)", seiteAContent, true)
                .AddField($"{siteBName} (Option B)", seiteBContent, true)
                .AddField($"Maximale Gewinnmöglichkeit", $"```{maxPayoutMultiplicator}X der Einsatz```")
                .AddField("❌ **Wette wurde abgebrochen**", $"<t:{unixTimestampCancelDate}:f>");
        }
        else
        {
            var siteABetAmounts = siteA.Select(a => a.betAmount).ToList();
            var siteBBetAmounts = siteB.Select(b => b.betAmount).ToList();
            var siteAQuota = await _calculatorService.CalculateBetQuotaFromBetAmounts(siteABetAmounts, siteBBetAmounts, maxPayoutMultiplicator);
            var siteBQuota = await _calculatorService.CalculateBetQuotaFromBetAmounts(siteBBetAmounts, siteABetAmounts, maxPayoutMultiplicator);
            var siteAQuotaString = siteAQuota == 0 ? "" : $"\nQuote:{siteAQuota} X";
            var siteBQuotaString = siteBQuota == 0 ? "" : $"\nQuote:{siteBQuota} X";
            
            
            embedBuilder
                .AddField($"{siteAName} (Option A)", seiteAContent, true)
                .AddField($"{siteBName} (Option B)", seiteBContent, true)
                .AddField($" Gesamteinsatz", $"**{siteA.Sum(x => x.betAmount) + siteB.Sum(x => x.betAmount)} XP**")
                .AddField($"Einsatz für **{siteAName}**",
                    $"```\n{siteA.Sum(x => x.betAmount)} XP {siteAQuotaString}```", true)
                .AddField($"Einsatz für **{siteBName}**",
                    $"```\n{siteB.Sum(x => x.betAmount)} XP {siteBQuotaString} ```", true)
                .AddField($"Maximale Gewinnmöglichkeit", $"```{maxPayoutMultiplicator}X der Einsatz```")
                .AddField("⏳ Wettannahme endet am", $"<t:{unixTimestamp}:f>");
        }

        return embedBuilder.Build();
    }
}