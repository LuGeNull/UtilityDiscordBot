namespace UtilsBot;

public class InterestedPerson
{
    public ulong UserId { get; }
    public ulong GuildId { get; }
    
    public int NichtBenachrichtigenZeitVon { get; set; }
    public int NichtBenachrichtigenZeitBis { get; set; }
    
    public int ImmerBenachrichtigen = -1;
    
    public DateTime LetztesMalBenachrichtigt { get; set; }

    public InterestedPerson(ulong UserId, ulong guildId,  int nichtBenachrichtigenZeitVon = -1, int nichtBenachrichtigenZeitBis = -1)
    {
        this.UserId = UserId;
        GuildId = guildId;
        NichtBenachrichtigenZeitVon = nichtBenachrichtigenZeitVon;
        NichtBenachrichtigenZeitBis = nichtBenachrichtigenZeitBis;
    }
    
    public override bool Equals(object obj)
    {
        return obj is InterestedPerson other &&
               UserId == other.UserId &&
               GuildId == other.GuildId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(UserId, GuildId);
    }
}