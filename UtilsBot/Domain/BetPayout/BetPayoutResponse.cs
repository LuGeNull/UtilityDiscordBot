namespace UtilsBot.Domain.BetPayout;

public record BetPayoutResponse(bool wetteIstNichtZuende, bool wetteExistiertNicht = false, bool wetteWurdeSchonBeendet = false);
