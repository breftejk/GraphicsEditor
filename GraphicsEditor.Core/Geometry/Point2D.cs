namespace GraphicsEditor.Core.Geometry;

/// <summary>
/// Represents a 2D point with X and Y coordinates.
/// </summary>
public struct Point2D : IEquatable<Point2D>
{
    public double X { get; set; }
    public double Y { get; set; }

    public Point2D(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Calculates the distance between this point and another point.
    /// </summary>
    public double DistanceTo(Point2D other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Calculates the distance from this point to a line segment.
    /// </summary>
    public double DistanceToLineSegment(Point2D lineStart, Point2D lineEnd)
    {
        double dx = lineEnd.X - lineStart.X;
        double dy = lineEnd.Y - lineStart.Y;
        
        if (dx == 0 && dy == 0)
        {
            return DistanceTo(lineStart);
        }

        double t = Math.Max(0, Math.Min(1, ((X - lineStart.X) * dx + (Y - lineStart.Y) * dy) / (dx * dx + dy * dy)));
        
        Point2D projection = new Point2D(
            lineStart.X + t * dx,
            lineStart.Y + t * dy
        );
        
        return DistanceTo(projection);
    }

    public bool Equals(Point2D other)
    {
        return Math.Abs(X - other.X) < 1e-6 && Math.Abs(Y - other.Y) < 1e-6;
    }

    public override bool Equals(object? obj)
    {
        return obj is Point2D other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public static bool operator ==(Point2D left, Point2D right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Point2D left, Point2D right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"({X:F2}, {Y:F2})";
    }
}
