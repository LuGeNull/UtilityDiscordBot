using System.ComponentModel.DataAnnotations;

namespace UtilsBot.Domain.Models;

public class BenachrichtigungEingegangen
{
    public ulong VersendetVonId { get; set; }
    public AllgemeinePerson VersendetVon { get; set; }
    
    public ulong EmpfangenVonId { get; set; }
    public AllgemeinePerson EmpfangenVon { get; set; }
    public DateTime AbsendeZeitpunkt { get; set; }
}