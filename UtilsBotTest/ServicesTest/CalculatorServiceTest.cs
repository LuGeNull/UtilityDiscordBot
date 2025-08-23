using UtilsBot.Services;

namespace UtilsBotTest.ServicesTest;
[TestClass]
public class CalculatorServiceTest
{
    private readonly CalculatorService _calculatorService = new CalculatorService();

    [TestMethod("Wenn einsaetze 0 dann 0 zurückgeben")]
    public async Task T1()
    {
        var seiteA = new List<int>();
        var seiteB = new List<int>();
        var ergebnis = await _calculatorService.CalculateBetQuotaFromBetAmounts(seiteA, seiteB, 3);
        Assert.IsTrue(ergebnis == 0);
    }
    
    [TestMethod("Wenn ein Team einsatz 0 dann 0 zurückgeben")]
    public async Task T2()
    {
        var seiteA = new List<int>()
        {
            100,
            200
        };
        var seiteB = new List<int>();
        var ergebnis = await _calculatorService.CalculateBetQuotaFromBetAmounts(seiteA, seiteB, 3);
        Assert.IsTrue(ergebnis == 0);
    }
    
    [TestMethod("Wenn beide teams gleich dann 2")]
    public async Task T3()
    {
        var seiteA = new List<int>()
        {
            100,
            200
        };
        var seiteB = new List<int>()
        {
            100,
            200
        };
        var ergebnis = await _calculatorService.CalculateBetQuotaFromBetAmounts(seiteA, seiteB, 3);
        Assert.IsTrue(ergebnis == 2);
    }
    
    [TestMethod("Wenn größer als Max Payout dann maxPayout zurückgeben")]
    public async Task T4()
    {
        var seiteA = new List<int>()
        {
            1
        };
        var seiteB = new List<int>()
        {
            100,
            200
        };
        var maxPayoutMultiplikator = 3;
        var ergebnis = await _calculatorService.CalculateBetQuotaFromBetAmounts(seiteA, seiteB, maxPayoutMultiplikator);
        Assert.IsTrue(ergebnis == maxPayoutMultiplikator);
    }
    
    [TestMethod("Mit Fliesskommazahl als ergebnis")]
    public async Task T5()
    {
        var seiteA = new List<int>()
        {
            2
        };
        var seiteB = new List<int>()
        {
            3
        };
        var maxPayoutMultiplikator = 3;
        var ergebnis = await _calculatorService.CalculateBetQuotaFromBetAmounts(seiteA, seiteB, maxPayoutMultiplikator);
        Assert.IsTrue(ergebnis == 2.5);
    }
    
    [TestMethod("Gibt höchstens 2 nachkommastellen zurück")]
    public async Task T6()
    {
        var seiteA = new List<int>()
        {
            4
        };
        var seiteB = new List<int>()
        {
            7
        };
        var maxPayoutMultiplikator = 3;
        var ergebnis = await _calculatorService.CalculateBetQuotaFromBetAmounts(seiteA, seiteB, maxPayoutMultiplikator);
        Assert.IsTrue(ergebnis == 2.75);
    }
}