using GraphicsEditor.Core.Models;

namespace GraphicsEditor.Core.Conversions;

/// <summary>
/// Provides conversion methods between RGB and HSV color spaces.
/// </summary>
public static class HsvConverter
{
    /// <summary>
    /// Converts RGB color to HSV color.
    /// </summary>
    public static HsvColor RgbToHsv(RgbColor rgb)
    {
        double r = rgb.R / 255.0;
        double g = rgb.G / 255.0;
        double b = rgb.B / 255.0;

        double max = Math.Max(Math.Max(r, g), b);
        double min = Math.Min(Math.Min(r, g), b);
        double delta = max - min;

        // Calculate Value
        double v = max * 100;

        // Calculate Saturation
        double s = max == 0 ? 0 : (delta / max) * 100;

        // Calculate Hue
        double h = 0;
        if (delta != 0)
        {
            if (max == r)
            {
                h = 60 * (((g - b) / delta) % 6);
            }
            else if (max == g)
            {
                h = 60 * (((b - r) / delta) + 2);
            }
            else if (max == b)
            {
                h = 60 * (((r - g) / delta) + 4);
            }

            if (h < 0)
                h += 360;
        }

        return new HsvColor(h, s, v);
    }

    /// <summary>
    /// Converts HSV color to RGB color.
    /// </summary>
    public static RgbColor HsvToRgb(HsvColor hsv)
    {
        double h = hsv.H;
        double s = hsv.S / 100.0;
        double v = hsv.V / 100.0;

        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
        double m = v - c;

        double r = 0, g = 0, b = 0;

        if (h >= 0 && h < 60)
        {
            r = c; g = x; b = 0;
        }
        else if (h >= 60 && h < 120)
        {
            r = x; g = c; b = 0;
        }
        else if (h >= 120 && h < 180)
        {
            r = 0; g = c; b = x;
        }
        else if (h >= 180 && h < 240)
        {
            r = 0; g = x; b = c;
        }
        else if (h >= 240 && h < 300)
        {
            r = x; g = 0; b = c;
        }
        else
        {
            r = c; g = 0; b = x;
        }

        return new RgbColor(
            (byte)Math.Clamp((r + m) * 255, 0, 255),
            (byte)Math.Clamp((g + m) * 255, 0, 255),
            (byte)Math.Clamp((b + m) * 255, 0, 255)
        );
    }
}
