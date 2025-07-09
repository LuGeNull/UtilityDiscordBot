namespace UtilsBot.Domain;

public class LevelLogic
{
    
    public int BerechneLevel(long xp, int basisXp = 1300, double faktor = 1.3)
    {
        int level = 1;
        double summe = basisXp;
        while (xp >= summe)
        {
            level++;
            summe += basisXp * Math.Pow(faktor, level - 1);
        }
        return level;
    }
    
    public int BerechneXpFuerLevel(int level, int basisXp = 1300, double faktor = 1.3)
    {
        double summe = 0;
        for (int i = 0; i < level; i++)
        {
            summe += basisXp * Math.Pow(faktor, i);
        }
        return (int)Math.Ceiling(summe);
    }
    
}