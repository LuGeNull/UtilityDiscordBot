using UtilsBot.Services;

namespace UtilsBotTest.ServicesTest;
[TestClass]
public class LevelServiceTest
{
    private readonly LevelService _levelService = new LevelService();

    [TestMethod("Level 1 braucht 0 XP")]
    public void T1()
    {
        Assert.AreEqual(0, _levelService.BerechneMinimumXpFuerLevel(1));
    }

    [TestMethod("Minimum-XP fuer Level ergibt beim Rueckrechnen genau dieses Level")]
    public void T2()
    {
        for (int level = 1; level <= 100; level++)
        {
            var minimumXp = _levelService.BerechneMinimumXpFuerLevel(level);
            var berechnetesLevel = _levelService.BerechneLevelUndRestXp(minimumXp);
            Assert.AreEqual(level, berechnetesLevel, $"Level {level}: MinimumXp {minimumXp} ergab Level {berechnetesLevel}");
        }
    }

    [TestMethod("Ein XP-Punkt unter dem Minimum ergibt das Level darunter")]
    public void T3()
    {
        for (int level = 2; level <= 100; level++)
        {
            var minimumXp = _levelService.BerechneMinimumXpFuerLevel(level);
            var berechnetesLevel = _levelService.BerechneLevelUndRestXp(minimumXp - 1);
            Assert.AreEqual(level - 1, berechnetesLevel, $"Level {level}: {minimumXp - 1} XP ergab Level {berechnetesLevel} statt {level - 1}");
        }
    }
}
