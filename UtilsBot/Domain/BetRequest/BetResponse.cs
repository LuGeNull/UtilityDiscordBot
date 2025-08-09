namespace UtilsBot.Domain.BetRequest;

public record BetResponse(bool existiertEineBet = true , bool userHatGenugXp = true, bool wetteBereitsVorbei = false, bool userWettetAufGegenseite= false, bool anfrageWarErfolgreich = true);