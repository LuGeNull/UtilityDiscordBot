namespace UtilsBot.Domain.Xp;

public record InfoResponse(int level, decimal xp, decimal xpToNextLevel, long platzDerPerson, long currentXpGain, int nachrichtenPunkte,  bool anfrageWarErfolgreich = true);