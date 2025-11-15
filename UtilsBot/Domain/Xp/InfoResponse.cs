namespace UtilsBot.Domain.Xp;

public record InfoResponse(int level, decimal xp, decimal xpToNextLevel, long platzDerPerson, long currentXpGain, decimal currentGoldGain, int nachrichtenPunkte,  bool anfrageWarErfolgreich = true);