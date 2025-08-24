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
        Assert.IsTrue(r == 0 && g == 112 && b == 210);
    }
    
    [TestMethod("Color changes by level")]
    public async Task T2()
    {
        var r = 0;
        var g = 0;
        var b = 0;
        var color = _colorService.GetColorFromLevel(1);
        var color2 = _colorService.GetColorFromLevel(2);
        
        Assert.IsTrue(color.Item1 == 6 && color.Item2 == 36 && color.Item3 == 187);
        Assert.IsTrue(color2.Item1 == 12 && color2.Item2 == 42 && color2.Item3 == 194);
       
    }
} 