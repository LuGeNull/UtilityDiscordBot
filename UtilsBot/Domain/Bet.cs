namespace UtilsBot.Domain;

public class Bet
{
    public Guid Id { get; set; }
    public int ReferenzId { get; set; }
    public int MaxPayoutMultiplikator { get; set; }
    public string Title { get; set; }
    public string Ereignis1Name { get; set; }
    public string Ereignis2Name { get; set; }
    public ulong UserIdStartedBet { get; set; }
    public bool WetteWurdeAbgebrochen { get; set; }
    public bool WetteWurdeBeendet { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public ICollection<BetPlacements> Placements { get; set; }
    public ulong MessageId { get; set; }
    public ulong ChannelId { get; set; }
}

