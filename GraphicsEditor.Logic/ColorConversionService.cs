using GraphicsEditor.Core.Models;
using GraphicsEditor.Core.Conversions;

namespace GraphicsEditor.Logic;

/// <summary>
/// Provides color conversion services between different color spaces.
/// </summary>
public class ColorConversionService
{
    /// <summary>
    /// Converts RGB to CMYK color space.
    /// </summary>
    public CmykColor ConvertRgbToCmyk(RgbColor rgb)
    {
        return RgbCmykConverter.RgbToCmyk(rgb);
    }

    /// <summary>
    /// Converts CMYK to RGB color space.
    /// </summary>
    public RgbColor ConvertCmykToRgb(CmykColor cmyk)
    {
        return RgbCmykConverter.CmykToRgb(cmyk);
    }

    /// <summary>
    /// Converts RGB to HSV color space.
    /// </summary>
    public HsvColor ConvertRgbToHsv(RgbColor rgb)
    {
        return HsvConverter.RgbToHsv(rgb);
    }

    /// <summary>
    /// Converts HSV to RGB color space.
    /// </summary>
    public RgbColor ConvertHsvToRgb(HsvColor hsv)
    {
        return HsvConverter.HsvToRgb(hsv);
    }

    /// <summary>
    /// Converts RGB color to hex string (#RRGGBB).
    /// </summary>
    public string RgbToHex(RgbColor rgb)
    {
        return $"#{rgb.R:X2}{rgb.G:X2}{rgb.B:X2}";
    }

    /// <summary>
    /// Converts hex string to RGB color.
    /// </summary>
    public RgbColor HexToRgb(string hex)
    {
        hex = hex.TrimStart('#');

        if (hex.Length != 6)
            throw new ArgumentException("Hex color must be in format #RRGGBB");

        byte r = Convert.ToByte(hex.Substring(0, 2), 16);
        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
        byte b = Convert.ToByte(hex.Substring(4, 2), 16);

        return new RgbColor(r, g, b);
    }
}
