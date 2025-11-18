using GraphicsEditor.Core.Geometry;
using System.Text;
using System.Text.Json.Serialization;

namespace GraphicsEditor.Core.Models;

public class BezierCurve : IShape
{
    public Guid Id { get; private set; } // allow deserializer to set via private setter
    public string Name { get; set; }
    public uint StrokeColor { get; set; }
    public uint? FillColor { get; set; }
    public double StrokeThickness { get; set; }
    public bool IsSelected { get; set; }

    // Make settable so System.Text.Json can assign the whole list on deserialization
    public List<Point2D> ControlPoints { get; set; } = new();

    [JsonIgnore]
    public int Degree => Math.Max(0, ControlPoints.Count - 1);

    public BezierCurve()
    {
        Id = Guid.NewGuid();
        Name = "Bezier";
        StrokeColor = 0xFF000000;
        StrokeThickness = 2.0;
        FillColor = null;
        IsSelected = false;
    }

    public BezierCurve(IEnumerable<Point2D> points) : this()
    {
        ControlPoints.AddRange(points);
    }

    public void AddPoint(Point2D p) => ControlPoints.Add(p);

    public object Clone()
    {
        var clone = new BezierCurve(ControlPoints)
        {
            Id = Guid.NewGuid(),
            Name = Name,
            StrokeColor = StrokeColor,
            FillColor = FillColor,
            StrokeThickness = StrokeThickness,
            IsSelected = false
        };
        return clone;
    }

    public bool HitTest(double x, double y, double tolerance = 5.0)
    {
        if (ControlPoints.Count < 2) return false;

        var b = GetBounds();
        if (x < b.X - tolerance || x > b.X + b.Width + tolerance || y < b.Y - tolerance || y > b.Y + b.Height + tolerance)
            return false;

        var sampled = SampleCurve(64);
        for (int i = 0; i < sampled.Count - 1; i++)
        {
            if (DistancePointToSegment(x, y, sampled[i].X, sampled[i].Y, sampled[i + 1].X, sampled[i + 1].Y) <= tolerance)
                return true;
        }
        
        foreach (var cp in ControlPoints)
        {
            if (Math.Abs(cp.X - x) <= tolerance && Math.Abs(cp.Y - y) <= tolerance)
                return true;
        }

        return false;
    }

    public void Move(double deltaX, double deltaY)
    {
        for (int i = 0; i < ControlPoints.Count; i++)
        {
            ControlPoints[i] = new Point2D(ControlPoints[i].X + deltaX, ControlPoints[i].Y + deltaY);
        }
    }

    public (double X, double Y, double Width, double Height) GetBounds()
    {
        if (ControlPoints.Count == 0) return (0, 0, 0, 0);
        double minX = ControlPoints.Min(p => p.X);
        double minY = ControlPoints.Min(p => p.Y);
        double maxX = ControlPoints.Max(p => p.X);
        double maxY = ControlPoints.Max(p => p.Y);
        return (minX, minY, maxX - minX, maxY - minY);
    }

    public List<Point2D> SampleCurve(int segments)
    {
        var pts = new List<Point2D>(segments + 1);
        if (ControlPoints.Count == 0)
            return pts;

        for (int i = 0; i <= segments; i++)
        {
            double t = i / (double)segments;
            pts.Add(Evaluate(t));
        }
        return pts;
    }

    public Point2D Evaluate(double t)
    {
        int n = ControlPoints.Count - 1;
        if (n < 0) return new Point2D(0, 0);
        if (n == 0) return ControlPoints[0];

        var tmp = new Point2D[n + 1];
        for (int i = 0; i <= n; i++) tmp[i] = ControlPoints[i];
        for (int r = 1; r <= n; r++)
        {
            for (int i = 0; i <= n - r; i++)
            {
                tmp[i] = new Point2D(
                    (1 - t) * tmp[i].X + t * tmp[i + 1].X,
                    (1 - t) * tmp[i].Y + t * tmp[i + 1].Y
                );
            }
        }
        return tmp[0];
    }

    private static double DistancePointToSegment(double px, double py, double x1, double y1, double x2, double y2)
    {
        double dx = x2 - x1, dy = y2 - y1;
        if (dx == 0 && dy == 0)
            return Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
        double t = ((px - x1) * dx + (py - y1) * dy) / (dx * dx + dy * dy);
        t = Math.Clamp(t, 0, 1);
        double cx = x1 + t * dx;
        double cy = y1 + t * dy;
        double ex = px - cx;
        double ey = py - cy;
        return Math.Sqrt(ex * ex + ey * ey);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append($"Bezier (deg {Degree}) CPs=");
        for (int i = 0; i < ControlPoints.Count; i++)
        {
            sb.Append($"({ControlPoints[i].X:F1},{ControlPoints[i].Y:F1})");
            if (i < ControlPoints.Count - 1) sb.Append(", ");
        }
        return sb.ToString();
    }
}
