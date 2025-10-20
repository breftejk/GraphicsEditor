namespace GraphicsEditor.Core.Models;

/// <summary>
/// Represents an HSV (Hue, Saturation, Value) color model.
/// H: 0-360 degrees, S: 0-100%, V: 0-100%
/// </summary>
public struct HsvColor : IEquatable<HsvColor>
{
    public double H { get; set; } // Hue (0-360)
    public double S { get; set; } // Saturation (0-100)
    public double V { get; set; } // Value (0-100)

    public HsvColor(double h, double s, double v)
    {
        H = h % 360;
        if (H < 0) H += 360;
        S = Math.Clamp(s, 0, 100);
        V = Math.Clamp(v, 0, 100);
    }

    public bool Equals(HsvColor other)
    {
        return Math.Abs(H - other.H) < 0.01 &&
               Math.Abs(S - other.S) < 0.01 &&
               Math.Abs(V - other.V) < 0.01;
    }

    public override bool Equals(object? obj)
    {
        return obj is HsvColor other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(H, S, V);
    }

    public static bool operator ==(HsvColor left, HsvColor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(HsvColor left, HsvColor right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"HSV({H:F1}°, {S:F1}%, {V:F1}%)";
    }
}
