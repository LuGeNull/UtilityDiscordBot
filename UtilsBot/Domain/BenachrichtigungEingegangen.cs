using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace UtilsBot;

public class BenachrichtigungEingegangen
{
    [Key]
    public Guid Id { get; set; }
    public ulong EingegangenVonUserID { get; set; }
    public string EingegangenVonDisplayName { get; set; }
    public DateTime EingegangenZeitpunkt { get; set; }
}