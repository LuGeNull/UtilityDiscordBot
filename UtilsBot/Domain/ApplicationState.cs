namespace UtilsBot.Domain;

public static class ApplicationState
{
    public static bool TestMode { get; set; }
    public static string Token => TestMode ? TestToken : ProdToken;
    public static string TestToken { get; set; }
    public static decimal DefaultGoldEarning => 0.4m;
    public static long StartXp => 350;
    public static double XpFaktorErhoehung => 1.1;
    public static string ProdToken { get; set; }
    public static int NachrichtenpunkteTaeglich => 500;
    public static int TickPerXSeconds => 60000;
    public static int BaseXp => 4;
    public static int StreamAndVideoBonus => 4;
    public static int StreamOrVideoBonus => 2;
    public static int FullMuteBaseXp => 1;
    public static int OnlyMuteBaseXp => 1;
    public static int NormalMessageXpGain => 10;
    public static int PictureMessageXpGain => 20;
    public static int LinkMessageXpGain => 15;
    public static int VideoMessageXpGain => 25;
    public static int GifMessageXpGain => 15;
    public static bool DeleteGuildRoles { get; set; }
}