using GraphicsEditor.Core.Geometry;

namespace GraphicsEditor.Core.Models;

/// <summary>
/// Represents a circle defined by a center point and radius.
/// </summary>
public class Circle : IShape
{
    public Guid Id { get; private set; }
    public string Name { get; set; }
    public uint StrokeColor { get; set; }
    public uint? FillColor { get; set; }
    public double StrokeThickness { get; set; }
    public bool IsSelected { get; set; }

    public Point2D Center { get; set; }
    public double Radius { get; set; }

    public Circle()
    {
        Id = Guid.NewGuid();
        Name = "Circle";
        StrokeColor = 0xFF000000; // Black
        StrokeThickness = 1.0;
    }

    public Circle(Point2D center, double radius) : this()
    {
        Center = center;
        Radius = radius;
    }

    public Circle(double centerX, double centerY, double radius) : this()
    {
        Center = new Point2D(centerX, centerY);
        Radius = radius;
    }

    public bool HitTest(double x, double y, double tolerance = 5.0)
    {
        Point2D point = new Point2D(x, y);
        double distance = point.DistanceTo(Center);

        if (FillColor.HasValue)
        {
            // For filled circles, check if point is inside
            return distance <= Radius;
        }
        else
        {
            // For non-filled circles, check if point is near the outline
            return Math.Abs(distance - Radius) <= tolerance;
        }
    }

    public void Move(double deltaX, double deltaY)
    {
        Center = new Point2D(Center.X + deltaX, Center.Y + deltaY);
    }

    /// <summary>
    /// Resizes the circle by changing its radius.
    /// </summary>
    public void Resize(double newRadius)
    {
        Radius = Math.Max(1, newRadius);
    }

    public (double X, double Y, double Width, double Height) GetBounds()
    {
        return (Center.X - Radius, Center.Y - Radius, Radius * 2, Radius * 2);
    }

    public object Clone()
    {
        return new Circle
        {
            Id = Guid.NewGuid(),
            Name = Name,
            StrokeColor = StrokeColor,
            FillColor = FillColor,
            StrokeThickness = StrokeThickness,
            IsSelected = false,
            Center = Center,
            Radius = Radius
        };
    }

    public override string ToString()
    {
        return $"{Name}: Center {Center}, Radius {Radius:F2}";
    }
}
