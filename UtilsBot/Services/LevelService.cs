namespace UtilsBot.Services;

public class LevelService
{
    public int BerechneLevel(double xp, double xpStart = 100)
    {
        if (xp < xpStart) return 1;
        return (int)(Math.Log(xp / xpStart) / Math.Log(1.3)) + 1;
    }
    
    
}