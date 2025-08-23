namespace UtilsBot.Services;

public class CalculatorService
{
    public async Task<double> CalculateBetQuotaFromBetAmounts(List<int> seiteAEinsaetze, List<int> seiteBEinsaetze,
        long maxPayoutMultiplikator)
    {
        var seiteASumme = (double)seiteAEinsaetze.Sum();
        var seiteBSumme = (double)seiteBEinsaetze.Sum();
        if (seiteASumme == 0 || seiteBSumme == 0)
        {
            return 0;
        }
        var berechnung = Math.Round((seiteASumme + seiteBSumme) / seiteASumme, 2);
        if (berechnung > maxPayoutMultiplikator)
        {
            return maxPayoutMultiplikator;
        }
        return Math.Round((seiteASumme + seiteBSumme) / seiteASumme, 2);
      
    }
}