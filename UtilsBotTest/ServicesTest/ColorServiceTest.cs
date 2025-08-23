using UtilsBot.Services;

namespace UtilsBotTest.ServicesTest;
[TestClass]
public class ColorServiceTest
{
    private readonly ColorService _colorService =  new ColorService();

    [TestMethod("Richtige Konvertierung von HSV zu RGB")]
    public async Task T1()
    {
        var r = 0;
        var g = 0;
        var b = 0;
        _colorService.HSVToRGB(208, 100, 82, out r, out g, out b);
        Assert.IsTrue(r == 0 && g == 112 && b == 209);
    }
} 