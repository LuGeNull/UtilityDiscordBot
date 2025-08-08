namespace UtilsBot.Domain.BetStart;

public record BetStartRequest(
    ulong userIdStartedBet,
    ulong guildId,
    string title,
    long annahmeschlussAbJetztInStunden,
    ulong messageId,
    ulong channelId,
    string ereignis1Name,
    string ereignis2Name,
    int maxPayoutMultiplikator);