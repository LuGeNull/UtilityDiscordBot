namespace UtilsBot.Domain.BetCancel;

public record BetCancelResponse(bool wetteIstNichtZuende, bool wetteExistiertNicht = false);