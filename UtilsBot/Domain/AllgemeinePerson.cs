using System.ComponentModel.DataAnnotations;

namespace UtilsBot.Domain;

public class AllgemeinePerson
{
    public ulong UserId { get; set;}
    public string DisplayName { get; set; }
    public ulong GuildId { get;set;  }
    public long Xp { get; set; }
    public int XpTodayByMessages { get; set; }
    public DateTime LastXpGainDate { get; set; }
    public long GetsSoMuchXpRightNow { get; set; }
    public DateTime LastTimeInChannel { get; set; }
}