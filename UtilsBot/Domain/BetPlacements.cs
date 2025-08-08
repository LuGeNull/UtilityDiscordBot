using System.ComponentModel.DataAnnotations;

namespace UtilsBot.Domain;

public class BetPlacements
{
    [Key]
    public Guid Id { get; set; }
    public ulong? UserId { get; set; }
    public string DisplayName { get; set; }
    public long Einsatz { get; set; }
    public bool Site { get; set; }
    public Guid BetId { get; set; } // Fremdschlüssel
    public Bet Bet { get; set; }   // Navigation Property
}