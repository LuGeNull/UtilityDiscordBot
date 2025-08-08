using UtilsBot.Domain.ValueObjects;

namespace UtilsBot.Domain.BetRequest;

public record BetRequest(ulong? messageId, ulong? userId, long einsatz, WettOption option);