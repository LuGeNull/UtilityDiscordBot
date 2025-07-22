namespace UtilsBot;

public static class ApplicationState
{
    public static bool TestMode { get; set; }
    public static string Token => TestMode ? TestToken : ProdToken;
    public static string TestToken { get; set; }
    public static long StartXp => 1000;
    public static double XpFaktorErhoehung => 1.3;
    public static string ProdToken { get; set; }
    public static bool KommandosAktiviert => true;
    public static int NachrichtenWerdenGeloeschtNachXMinuten => 5;
    public static int NachrichtenpunkteTaeglich => 500;
    public static int TickProXSekunden => 60000;
    public static int BaseXp => 4;
    public static int StreamAndVideoBonus => 4;
    public static int StreamOrVideoBonus => 2;
    public static int VideoOnlyBonus => 2;

    public static int FullMuteBaseXp => 2;
    public static int OnlyMuteBaseXp => 3;
    public static int NormalMessageXpGain => 10;
    public static int PictureMessageXpGain => 20;
    public static int LinkMessageXpGain => 15;
    public static int VideoMessageXpGain => 25;
    public static int GifMessageXpGain => 15;
}