using System.ComponentModel.DataAnnotations;

namespace UtilsBot.Domain;

public class AllgemeinePerson
{
    [Key]
    public ulong UserId { get; set;}
    public string DisplayName { get; set; }
    public ulong GuildId { get;set;  }
    public long Xp { get; set; }
    public decimal Gold { get; set; }
    public int XpTodayByMessages { get; set; }
    public DateTime LastXpGainDate { get; set; }
    public long GetsSoMuchXpRightNow { get; set; }
    public DateTime LastTimeInChannel { get; set; }
    public override bool Equals(object obj)
    {
        return obj is AllgemeinePerson other &&
               UserId == other.UserId &&
               GuildId == other.GuildId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(UserId, GuildId);
    }
}