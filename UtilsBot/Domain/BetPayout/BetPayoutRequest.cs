using UtilsBot.Domain.ValueObjects;

namespace UtilsBot.Domain.BetPayout;

public record BetPayoutRequest(ulong messageId, BetSide betSide);