namespace UtilsBot.Domain.BetRequest;

public record BetResponse(bool existiertEineBet = true , bool userHatGenugGold = true, bool BetIsAlreadyClosed = false, bool userBetsOnBothSides= false, bool requestWasSuccesful = true);