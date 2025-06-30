namespace UtilsBot;

public class InterestedPeople
{
    public ulong UserId { get; }
    public ulong GuildId { get; }
    
    public DateTime NichtBenachrichtigenZeitVon { get; set; }
    public DateTime NichtBenachrichtigenZeitBis { get; set; }

    public InterestedPeople(ulong UserId, ulong guildId)
    {
        this.UserId = UserId;
        GuildId = guildId;
    }

    public override bool Equals(object obj)
    {
        return obj is InterestedPeople other &&
               UserId == other.UserId &&
               GuildId == other.GuildId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(UserId, GuildId);
    }
}