namespace UtilsBot;

public static class ApplicationState
{
    public static bool TestMode { get; set; }
    public static bool KommandosAktiviert { get; set; }
    public static int NachrichtenWerdenGeloeschtNachXMinuten { get; set; } 
    
    public static int TickProXSekunden { get; set; }
    public static bool NachrichtenVerschicken { get; set; }
    public static int BaseXp { get; set; }
    public static int UserXMinutenAusDemChannel { get; set; }
    public static int StreamAndVideoBonus { get; set; }
    public static int StreamOrVideoBonus { get; set; }    
    public static int VideoOnlyBonus { get; set; }    
    
    public static int FullMuteBaseXp { get; set; }
    public static int OnlyMuteBaseXp { get; set; }
}