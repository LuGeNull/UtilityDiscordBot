namespace UtilsBot;

public static class ApplicationState
{
    public static bool TestMode { get; set; }
    public static int MindestestAnzahlAnMinutenBevorWiederBenachrichtigtWird { get; set; } 
    public static int NachrichtenWerdenGeloeschtNachXMinuten { get; set; } 
}