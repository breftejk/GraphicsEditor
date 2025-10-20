using GraphicsEditor.Core.Models;

namespace GraphicsEditor.Core.Conversions;

/// <summary>
/// Provides conversion methods between RGB and CMYK color spaces.
/// </summary>
public static class RgbCmykConverter
{
    /// <summary>
    /// Converts RGB color to CMYK color using standard conversion formulas.
    /// </summary>
    public static CmykColor RgbToCmyk(RgbColor rgb)
    {
        // Normalize RGB values to 0-1 range
        double r = rgb.R / 255.0;
        double g = rgb.G / 255.0;
        double b = rgb.B / 255.0;

        // Calculate K (black)
        double k = 1.0 - Math.Max(Math.Max(r, g), b);

        // Handle black color (K = 1)
        if (k >= 0.9999)
        {
            return new CmykColor(0, 0, 0, 100);
        }

        // Calculate CMY
        double c = (1.0 - r - k) / (1.0 - k);
        double m = (1.0 - g - k) / (1.0 - k);
        double y = (1.0 - b - k) / (1.0 - k);

        // Convert to percentage
        return new CmykColor(
            c * 100,
            m * 100,
            y * 100,
            k * 100
        );
    }

    /// <summary>
    /// Converts CMYK color to RGB color using standard conversion formulas.
    /// </summary>
    public static RgbColor CmykToRgb(CmykColor cmyk)
    {
        // Normalize CMYK values to 0-1 range
        double c = cmyk.C / 100.0;
        double m = cmyk.M / 100.0;
        double y = cmyk.Y / 100.0;
        double k = cmyk.K / 100.0;

        // Convert to RGB
        double r = (1.0 - c) * (1.0 - k);
        double g = (1.0 - m) * (1.0 - k);
        double b = (1.0 - y) * (1.0 - k);

        return new RgbColor(
            (byte)Math.Clamp(r * 255, 0, 255),
            (byte)Math.Clamp(g * 255, 0, 255),
            (byte)Math.Clamp(b * 255, 0, 255)
        );
    }
}
