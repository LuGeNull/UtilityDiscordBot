namespace UtilsBot.Domain.BetRequest;

public record BetResponse(bool existiertEineBet, bool userHatGenugXp = true, bool wetteBereitsVorbei = false, bool userWettetAufGegenseite= false);