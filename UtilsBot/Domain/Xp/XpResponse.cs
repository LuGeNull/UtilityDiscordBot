namespace UtilsBot.Domain.Xp;

public record XpResponse(int level, long xp, long xpToNextLevel, long platzDerPerson, long currentGain, int nachrichtenPunkte,  bool anfrageWarErfolgreich = true);