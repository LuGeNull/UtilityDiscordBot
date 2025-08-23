namespace UtilsBot.Services;

public class ColorService
{
    public (int, int, int) GetColorFromLevel(int level)
    {
        var startingPointH = 230;
        var startingPointS = 100;
        var startingPointV = 70;
        
        int tens = level / 10; 
        int ones = level % 10;
        
        startingPointH = (startingPointH + (30 * tens)) % 360;       
        startingPointS =  startingPointS - (3 * ones);
        startingPointV =  startingPointV + (3 * ones);
        var r = 0;
        var g = 0;
        var b = 0;
        HSVToRGB(startingPointH, startingPointS, startingPointV, out r, out g, out b);
        return (r, g, b);
    }
    
    /// <summary>
    /// Converts HSV color values to RGB
    /// </summary>
    /// <param name="h">0 - 360</param>
    /// <param name="s">0 - 100</param>
    /// <param name="v">0 - 100</param>
    /// <param name="r">0 - 255</param>
    /// <param name="g">0 - 255</param>
    /// <param name="b">0 - 255</param>
    public void HSVToRGB(int h, int s, int v, out int r, out int g, out int b)
    {
        var rgb = new int[3];

        var baseColor = (h + 60) % 360 / 120;
        var shift = (h + 60) % 360 - (120 * baseColor + 60 );
        var secondaryColor = (baseColor + (shift >= 0 ? 1 : -1) + 3) % 3;
    
        //Setting Hue
        rgb[baseColor] = 255;
        rgb[secondaryColor] = (int) ((Math.Abs(shift) / 60.0f) * 255.0f);
    
        //Setting Saturation
        for (var i = 0; i < 3; i++)
            rgb[i] += (int) ((255 - rgb[i]) * ((100 - s) / 100.0f));
    
        //Setting Value
        for (var i = 0; i < 3; i++)
            rgb[i] -= (int) (rgb[i] * (100-v) / 100.0f);

        r = rgb[0];
        g = rgb[1];
        b = rgb[2];
    }
}