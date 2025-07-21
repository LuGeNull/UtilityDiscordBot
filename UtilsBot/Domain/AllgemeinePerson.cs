using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices.JavaScript;

namespace UtilsBot;

public class AllgemeinePerson
{
    [Key]
    public ulong UserId { get; set;}
    public string DisplayName { get; set; }
    public ulong GuildId { get;set;  }
    public long Xp { get; set; }
    public int XpTodayByMessages { get; set; }
    public DateTime LastXpGainDate { get; set; }
    public long BekommtZurzeitSoVielXp { get; set; }
    public DateTime ZuletztImChannel { get; set; }
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