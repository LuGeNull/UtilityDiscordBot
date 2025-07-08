namespace UtilsBot.Domain.Models;

public class BotConfig
{
    public bool TestMode { get; init; }
    public bool KommandosAktiviert { get; init; }
    public int NachrichtenWerdenGelöschtNachXMinuten { get; init; }
    public int TickProXSekunden { get; init; }
    public bool NachrichtenVerschicken { get; init; }
    public int BaseXp { get; init; }
    public int UserXMinutenAusDemChannel { get; init; }
    public int StreamAndVideoBonus { get; init; }
    public int StreamOrVideoBonus { get; init; }
    public int VideoOnlyBonus { get; init; }
    public int FullMuteBaseXp { get; init; }
    public int OnlyMuteBaseXp { get; init; }
}