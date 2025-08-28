using UtilsBot.Domain.ValueObjects;

namespace UtilsBot.Domain.BetRequest;

public record BetRequest(ulong? messageId, ulong? userId, ulong? guildId, long einsatz, BetSide option);