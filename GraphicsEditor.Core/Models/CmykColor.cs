namespace GraphicsEditor.Core.Models;

/// <summary>
/// Represents a CMYK color model with Cyan, Magenta, Yellow, and Key (black) components (0-100%).
/// </summary>
public struct CmykColor : IEquatable<CmykColor>
{
    public double C { get; set; } // Cyan (0-100)
    public double M { get; set; } // Magenta (0-100)
    public double Y { get; set; } // Yellow (0-100)
    public double K { get; set; } // Key/Black (0-100)

    public CmykColor(double c, double m, double y, double k)
    {
        C = Math.Clamp(c, 0, 100);
        M = Math.Clamp(m, 0, 100);
        Y = Math.Clamp(y, 0, 100);
        K = Math.Clamp(k, 0, 100);
    }

    public bool Equals(CmykColor other)
    {
        return Math.Abs(C - other.C) < 0.01 &&
               Math.Abs(M - other.M) < 0.01 &&
               Math.Abs(Y - other.Y) < 0.01 &&
               Math.Abs(K - other.K) < 0.01;
    }

    public override bool Equals(object? obj)
    {
        return obj is CmykColor other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(C, M, Y, K);
    }

    public static bool operator ==(CmykColor left, CmykColor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CmykColor left, CmykColor right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"CMYK({C:F1}%, {M:F1}%, {Y:F1}%, {K:F1}%)";
    }
}
