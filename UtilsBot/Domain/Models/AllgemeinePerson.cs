using System.ComponentModel.DataAnnotations;

namespace UtilsBot.Domain.Models;

public class AllgemeinePerson
{
    [Key]
    public ulong UserId { get; set;}
    public string DisplayName { get; set; }
    public ulong GuildId { get;set;  }
    public bool WillBenachrichtigungenBekommen { get; set; }
    public long Xp { get; set; }
    public long BekommtZurzeitSoVielXp { get; set; }
    public DateTime ZuletztImChannel { get; set; }

  
    public long BenachrichtigenZeitVon { get; set; }
    public long BenachrichtigenZeitBis { get; set; }

    public AllgemeinePerson()
    {
        
    }
    public AllgemeinePerson(ulong userId, string displayName, ulong guildId)
    {
        this.UserId = userId;
        DisplayName = displayName;
        GuildId = guildId;
    }
   
    public bool KannUndWilldiePersonBenachrichtigtWerden(ulong userIdDerBenachrichtigendenPerson, string displayNameDerBenachrichtigendenPerson)
    {
        //if (WillBenachrichtigungenBekommen)
        //{
        //    var datumJetzt = DateTime.Now;
        //    var untereSchranke = new DateTime(datumJetzt.Year,datumJetzt.Month,datumJetzt.Day,int.Parse(BenachrichtigenZeitVon.ToString()), 0, 0);
        //    var obereSchranke = DateTime.Now;
        //    if (BenachrichtigenZeitBis < BenachrichtigenZeitVon)
        //    {
        //        untereSchranke = new DateTime(datumJetzt.Year, datumJetzt.Month, datumJetzt.Day, int.Parse(BenachrichtigenZeitVon.ToString()), 0, 0).AddDays(-1);
        //        obereSchranke = new DateTime(datumJetzt.Year, datumJetzt.Month, datumJetzt.Day, int.Parse(BenachrichtigenZeitBis.ToString()), 0, 0);
        //    }
        //    if (BenachrichtigenZeitBis == 24 || BenachrichtigenZeitBis == 0)
        //    {
        //        obereSchranke = new DateTime(datumJetzt.Year,datumJetzt.Month,datumJetzt.Day,23, 59, 59);
        //        
        //    }
        //    else
        //    {
        //        obereSchranke = new DateTime(datumJetzt.Year,datumJetzt.Month,datumJetzt.Day,int.Parse(BenachrichtigenZeitBis.ToString()), 0, 0);
        //    }
        //    
        //    if (datumJetzt > untereSchranke && datumJetzt < obereSchranke)
        //    {
        //        var aktuelleBenachrichtigungen = BenachrichtigungEingegangen.FirstOrDefault(b => b.VersendetVon == userIdDerBenachrichtigendenPerson);
        //        
        //        if (aktuelleBenachrichtigungen == null || aktuelleBenachrichtigungen.AbsendeZeitpunkt.AddMinutes(90) < DateTime.Now)
        //        {
        //            BenachrichtigungEingegangen.Remove(aktuelleBenachrichtigungen);
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //        
        //       // BenachrichtigungEingegangen.Add(new BenachrichtigungEingegangen
        //       // {
        //       //     VersendetVon = userIdDerBenachrichtigendenPerson,
        //       //     EingegangenVonDisplayName = displayNameDerBenachrichtigendenPerson,
        //       //     AbsendeZeitpunkt = DateTime.Now
        //       // });
        //        return true;
        //    }
        //    
        //}
        return false;
    }
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