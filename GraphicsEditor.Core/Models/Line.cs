using GraphicsEditor.Core.Geometry;

namespace GraphicsEditor.Core.Models;

/// <summary>
/// Represents a line segment defined by two endpoints.
/// </summary>
public class Line : IShape
{
    public Guid Id { get; private set; }
    public string Name { get; set; }
    public uint StrokeColor { get; set; }
    public uint? FillColor { get; set; }
    public double StrokeThickness { get; set; }
    public bool IsSelected { get; set; }

    public Point2D StartPoint { get; set; }
    public Point2D EndPoint { get; set; }

    public Line()
    {
        Id = Guid.NewGuid();
        Name = "Line";
        StrokeColor = 0xFF000000; // Black
        StrokeThickness = 1.0;
    }

    public Line(Point2D start, Point2D end) : this()
    {
        StartPoint = start;
        EndPoint = end;
    }

    public Line(double x1, double y1, double x2, double y2) : this()
    {
        StartPoint = new Point2D(x1, y1);
        EndPoint = new Point2D(x2, y2);
    }

    public bool HitTest(double x, double y, double tolerance = 5.0)
    {
        Point2D point = new Point2D(x, y);
        double distance = point.DistanceToLineSegment(StartPoint, EndPoint);
        return distance <= tolerance;
    }

    public void Move(double deltaX, double deltaY)
    {
        StartPoint = new Point2D(StartPoint.X + deltaX, StartPoint.Y + deltaY);
        EndPoint = new Point2D(EndPoint.X + deltaX, EndPoint.Y + deltaY);
    }

    public (double X, double Y, double Width, double Height) GetBounds()
    {
        double minX = Math.Min(StartPoint.X, EndPoint.X);
        double minY = Math.Min(StartPoint.Y, EndPoint.Y);
        double maxX = Math.Max(StartPoint.X, EndPoint.X);
        double maxY = Math.Max(StartPoint.Y, EndPoint.Y);

        return (minX, minY, maxX - minX, maxY - minY);
    }

    public object Clone()
    {
        return new Line
        {
            Id = Guid.NewGuid(),
            Name = Name,
            StrokeColor = StrokeColor,
            FillColor = FillColor,
            StrokeThickness = StrokeThickness,
            IsSelected = false,
            StartPoint = StartPoint,
            EndPoint = EndPoint
        };
    }

    public override string ToString()
    {
        return $"{Name}: {StartPoint} -> {EndPoint}";
    }
}
