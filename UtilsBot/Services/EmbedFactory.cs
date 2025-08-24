using Discord;
using UtilsBot.Domain;
using UtilsBot.Domain.ValueObjects;
using UtilsBot.Domain.Xp;

namespace UtilsBot.Services;

public class EmbedFactory
{
    private readonly CalculatorService _calculatorService;
    public EmbedFactory(CalculatorService calculatorService)
    {
        _calculatorService = calculatorService;
    }

    public async Task<Embed> BuildInfoEmbed(InfoResponse infoResponse)
    {
        return new EmbedBuilder()
            .WithTitle("Dein Fortschritt")
            .WithColor(Color.DarkRed)
            .AddField("Level", $"```{infoResponse.level}```", true)
            .AddField("XP", $"```{infoResponse.xp}```", true)
            .AddField("Gold", $"```{infoResponse.gold}```", true)
            .AddField($"XP bis Level {infoResponse.level + 1}", $"```{infoResponse.xpToNextLevel}```")
            .AddField("Dein Platz im Vergleich zu allen", $"```{infoResponse.platzDerPerson}```")
            .AddField($"Du bekommst zurzeit", $"```{infoResponse.currentXpGain} XP / MIN | {infoResponse.currentGoldGain.ToString().Replace(',', '.')} GOLD / MIN```")
            .AddField($"Nachrichtenpunkte heute verdient",
                $"```{infoResponse.nachrichtenPunkte} XP / {ApplicationState.MessagePointsDaily} XP```")
            .Build();
    }
    
    public async Task<Embed> BuildBetEmbed(string betTitle, string siteAName,
        List<(string user, int betAmount, long goldWon, long goldRefunded)> siteA,
        string siteBName, List<(string user, int betAmount, long goldWon, long goldRefunded)> siteB,
        long endOfBetInHours,DateTime betEndedAt,
        bool betIsBeingCanceled = false, long maxPayoutMultiplicator = 3,
        bool betIsInPayout = false, BetSide winningSide = BetSide.Yes)
    {
         var embedBuilder = new EmbedBuilder()
            .WithTitle($"{betTitle}")
            .WithColor(Color.DarkBlue);

        string usersA = "";
        string usersB = "";
        
        if (betIsBeingCanceled)
        {
            usersA = siteA.Count == 0
                ? "⚠️ Keine Teilnehmer"
                : string.Join("\n", siteA.Select(x => $"{x.user}: {x.betAmount} 🪙 (refunded)"));
            usersB = siteB.Count == 0
                ? "⚠️ Keine Teilnehmer"
                : string.Join("\n", siteB.Select(x => $"{x.user}: {x.betAmount} 🪙 (refunded)"));
        }
        else if (betIsInPayout)
        {
            usersA = string.Join("\n", siteA.Select(x => $"{x.user}: {x.betAmount} 🪙 {ErmittleGewinnOderRefund(x)}"));
            usersB = string.Join("\n", siteB.Select(x => $"{x.user}: {x.betAmount} 🪙 {ErmittleGewinnOderRefund(x)}"));
        }
        else if(!betIsBeingCanceled && !betIsInPayout)
        {
            usersA = siteA.Count == 0
                ? "Keine Teilnehmer"
                : string.Join("\n", siteA.Select(x => $"{x.user}: {x.betAmount} 🪙"));
            usersB = siteB.Count == 0
                ? "Keine Teilnehmer"
                : string.Join("\n", siteB.Select(x => $"{x.user}: {x.betAmount} 🪙"));
        }
      

        string seiteAContent = $"```{usersA}```";
        string seiteBContent = $"```{usersB}```";
        var unixTimestamp = 0L;
        if (betEndedAt == DateTime.MinValue)
        {
            var closingDate = DateTime.UtcNow.AddHours(endOfBetInHours);
            unixTimestamp = ((DateTimeOffset)closingDate).ToUnixTimeSeconds();
        }
        else
        {
            var closingDate = betEndedAt;
            unixTimestamp = ((DateTimeOffset)closingDate).ToUnixTimeSeconds();
        }
        

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
            var siteAQuotaString = siteAQuota == 0 ? "" : $"\nQuote: {siteAQuota} X";
            var siteBQuotaString = siteBQuota == 0 ? "" : $"\nQuote: {siteBQuota} X";
            
            
            embedBuilder
                .AddField($"{siteAName} (Option A)", seiteAContent, true)
                .AddField($"{siteBName} (Option B)", seiteBContent, true)
                .AddField($" Gesamteinsatz", $"**{siteA.Sum(x => x.betAmount) + siteB.Sum(x => x.betAmount)} 🪙**")
                .AddField($"Einsatz für **{siteAName}**",
                    $"```\n{siteA.Sum(x => x.betAmount)}🪙 {siteAQuotaString}```", true)
                .AddField($"Einsatz für **{siteBName}**",
                    $"```\n{siteB.Sum(x => x.betAmount)}🪙 {siteBQuotaString} ```", true)
                .AddField($"Maximale Gewinnmöglichkeit", $"```{maxPayoutMultiplicator}X der Einsatz```")
                .AddField("⏳ Wettannahme endet am", $"<t:{unixTimestamp}:f>");
        }

        return embedBuilder.Build();
    }

    private string ErmittleGewinnOderRefund((string user, int betAmount, long goldWon, long goldRefunded) userInfo)
    {
        if (userInfo.goldWon != 0)
        {
            return $"({userInfo.goldWon}🪙 won)";
        }
        if (userInfo.goldRefunded != 0)
        {
            return $"({userInfo.goldRefunded}🪙 refund)";
        }

        return "";
    }
}