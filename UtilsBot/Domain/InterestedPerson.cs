namespace UtilsBot;

public class InterestedPerson
{
    public ulong UserId { get; }
    public string DisplayName { get; }
    public ulong GuildId { get; }
    
    public long NichtBenachrichtigenZeitVon { get; set; }
    public long NichtBenachrichtigenZeitBis { get; set; }
    
    public DateTime LetztesMalBenachrichtigt { get; set; }

    public InterestedPerson(ulong UserId, string displayName, ulong guildId,
        long nichtBenachrichtigenZeitVon = 0, long nichtBenachrichtigenZeitBis = 24)
    {
        this.UserId = UserId;
        DisplayName = displayName;
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