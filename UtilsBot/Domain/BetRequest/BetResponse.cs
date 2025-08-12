namespace UtilsBot.Domain.BetRequest;

public record BetResponse(bool existiertEineBet = true , bool userHatGenugXp = true, bool BetIsAlreadyClosed = false, bool userBetsOnBothSides= false, bool requestWasSuccesful = true);