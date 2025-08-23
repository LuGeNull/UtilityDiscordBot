namespace UtilsBot.Domain.BetPayout;

public record BetPayoutResponse(bool betIsNotFinished, bool betDoesNotExist = false, bool BetWasAlreadyClosed = false, bool containsBetsOnlyOnOneSide = false, bool anfrageWarErfolgreich = true);
