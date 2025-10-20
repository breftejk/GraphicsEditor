namespace GraphicsEditor.Core.Models;

/// <summary>
/// Represents an RGB color model with Red, Green, Blue components (0-255).
/// </summary>
public struct RgbColor : IEquatable<RgbColor>
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }

    public RgbColor(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    /// <summary>
    /// Creates an RGB color from normalized values (0.0 - 1.0).
    /// </summary>
    public static RgbColor FromNormalized(double r, double g, double b)
    {
        return new RgbColor(
            (byte)Math.Clamp(r * 255, 0, 255),
            (byte)Math.Clamp(g * 255, 0, 255),
            (byte)Math.Clamp(b * 255, 0, 255)
        );
    }

    /// <summary>
    /// Converts to 32-bit ARGB color (with full alpha).
    /// </summary>
    public uint ToArgb()
    {
        return 0xFF000000 | ((uint)R << 16) | ((uint)G << 8) | B;
    }

    /// <summary>
    /// Creates RGB color from 32-bit ARGB value.
    /// </summary>
    public static RgbColor FromArgb(uint argb)
    {
        return new RgbColor(
            (byte)((argb >> 16) & 0xFF),
            (byte)((argb >> 8) & 0xFF),
            (byte)(argb & 0xFF)
        );
    }

    public bool Equals(RgbColor other)
    {
        return R == other.R && G == other.G && B == other.B;
    }

    public override bool Equals(object? obj)
    {
        return obj is RgbColor other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B);
    }

    public static bool operator ==(RgbColor left, RgbColor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RgbColor left, RgbColor right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"RGB({R}, {G}, {B})";
    }
}
