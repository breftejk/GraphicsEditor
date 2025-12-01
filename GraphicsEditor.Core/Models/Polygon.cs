using GraphicsEditor.Core.Geometry;
using System.Text;
using System.Text.Json.Serialization;

namespace GraphicsEditor.Core.Models;

/// <summary>
/// Represents a polygon defined by a list of vertices.
/// Supports homogeneous coordinate transformations.
/// </summary>
public class Polygon : IShape
{
    public Guid Id { get; private set; }
    public string Name { get; set; }
    public uint StrokeColor { get; set; }
    public uint? FillColor { get; set; }
    public double StrokeThickness { get; set; }
    public bool IsSelected { get; set; }

    /// <summary>
    /// The vertices of the polygon in local coordinates.
    /// </summary>
    public List<Point2D> Vertices { get; set; } = new();

    /// <summary>
    /// The transformation matrix applied to the polygon.
    /// </summary>
    public Matrix3x3 Transform { get; set; } = Matrix3x3.Identity;

    /// <summary>
    /// Gets the number of vertices in the polygon.
    /// </summary>
    [JsonIgnore]
    public int VertexCount => Vertices.Count;

    /// <summary>
    /// Indicates whether the polygon is closed (has at least 3 vertices).
    /// </summary>
    [JsonIgnore]
    public bool IsClosed => Vertices.Count >= 3;

    public Polygon()
    {
        Id = Guid.NewGuid();
        Name = "Polygon";
        StrokeColor = 0xFF000000; // Black
        StrokeThickness = 2.0;
        FillColor = null;
        IsSelected = false;
    }

    public Polygon(IEnumerable<Point2D> vertices) : this()
    {
        Vertices.AddRange(vertices);
    }

    /// <summary>
    /// Adds a vertex to the polygon.
    /// </summary>
    public void AddVertex(Point2D vertex)
    {
        Vertices.Add(vertex);
    }

    /// <summary>
    /// Adds a vertex at the specified coordinates.
    /// </summary>
    public void AddVertex(double x, double y)
    {
        Vertices.Add(new Point2D(x, y));
    }

    /// <summary>
    /// Gets the transformed vertices using the current transform matrix.
    /// </summary>
    public List<Point2D> GetTransformedVertices()
    {
        var result = new List<Point2D>(Vertices.Count);
        foreach (var vertex in Vertices)
        {
            result.Add(Transform.Transform(vertex));
        }
        return result;
    }

    /// <summary>
    /// Applies a transformation to the polygon.
    /// </summary>
    public void ApplyTransform(Matrix3x3 transform)
    {
        Transform = transform * Transform;
    }

    /// <summary>
    /// Resets the transform to identity and bakes the current transform into vertices.
    /// </summary>
    public void BakeTransform()
    {
        var transformed = GetTransformedVertices();
        Vertices.Clear();
        Vertices.AddRange(transformed);
        Transform = Matrix3x3.Identity;
    }

    /// <summary>
    /// Resets the transform to identity without baking.
    /// </summary>
    public void ResetTransform()
    {
        Transform = Matrix3x3.Identity;
    }

    /// <summary>
    /// Gets the centroid (center of mass) of the polygon.
    /// </summary>
    [JsonIgnore]
    public Point2D Centroid
    {
        get
        {
            if (Vertices.Count == 0) return new Point2D(0, 0);
            
            var transformed = GetTransformedVertices();
            double sumX = 0, sumY = 0;
            foreach (var v in transformed)
            {
                sumX += v.X;
                sumY += v.Y;
            }
            return new Point2D(sumX / transformed.Count, sumY / transformed.Count);
        }
    }

    public bool HitTest(double x, double y, double tolerance = 5.0)
    {
        if (Vertices.Count < 2) return false;

        var transformed = GetTransformedVertices();
        var point = new Point2D(x, y);

        // Quick bounds check
        var bounds = GetBounds();
        if (x < bounds.X - tolerance || x > bounds.X + bounds.Width + tolerance ||
            y < bounds.Y - tolerance || y > bounds.Y + bounds.Height + tolerance)
        {
            return false;
        }

        // Check if point is on any edge
        for (int i = 0; i < transformed.Count; i++)
        {
            var p1 = transformed[i];
            var p2 = transformed[(i + 1) % transformed.Count];
            
            if (point.DistanceToLineSegment(p1, p2) <= tolerance)
                return true;
        }

        // Check if point is near any vertex
        foreach (var v in transformed)
        {
            if (point.DistanceTo(v) <= tolerance)
                return true;
        }

        // Check if point is inside filled polygon
        if (FillColor.HasValue && IsPointInsidePolygon(x, y, transformed))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if a point is inside the polygon using ray casting algorithm.
    /// </summary>
    private bool IsPointInsidePolygon(double x, double y, List<Point2D> vertices)
    {
        if (vertices.Count < 3) return false;

        bool inside = false;
        for (int i = 0, j = vertices.Count - 1; i < vertices.Count; j = i++)
        {
            var vi = vertices[i];
            var vj = vertices[j];

            if ((vi.Y > y) != (vj.Y > y) &&
                x < (vj.X - vi.X) * (y - vi.Y) / (vj.Y - vi.Y) + vi.X)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    public void Move(double deltaX, double deltaY)
    {
        // Apply translation using homogeneous coordinates
        var translation = Matrix3x3.CreateTranslation(deltaX, deltaY);
        Transform = translation * Transform;
    }

    /// <summary>
    /// Rotates the polygon around a specified pivot point.
    /// </summary>
    /// <param name="pivot">The point to rotate around</param>
    /// <param name="angleRadians">Rotation angle in radians</param>
    public void Rotate(Point2D pivot, double angleRadians)
    {
        var rotation = Matrix3x3.CreateRotationAround(pivot, angleRadians);
        Transform = rotation * Transform;
    }

    /// <summary>
    /// Scales the polygon around a specified pivot point.
    /// </summary>
    /// <param name="pivot">The point to scale around</param>
    /// <param name="scaleFactor">Scale factor</param>
    public void Scale(Point2D pivot, double scaleFactor)
    {
        var scaling = Matrix3x3.CreateScaleAround(pivot, scaleFactor);
        Transform = scaling * Transform;
    }

    /// <summary>
    /// Scales the polygon non-uniformly around a specified pivot point.
    /// </summary>
    /// <param name="pivot">The point to scale around</param>
    /// <param name="scaleX">Scale factor in X direction</param>
    /// <param name="scaleY">Scale factor in Y direction</param>
    public void Scale(Point2D pivot, double scaleX, double scaleY)
    {
        var scaling = Matrix3x3.CreateScaleAround(pivot, scaleX, scaleY);
        Transform = scaling * Transform;
    }

    public (double X, double Y, double Width, double Height) GetBounds()
    {
        if (Vertices.Count == 0) return (0, 0, 0, 0);

        var transformed = GetTransformedVertices();
        double minX = transformed.Min(p => p.X);
        double minY = transformed.Min(p => p.Y);
        double maxX = transformed.Max(p => p.X);
        double maxY = transformed.Max(p => p.Y);

        return (minX, minY, maxX - minX, maxY - minY);
    }

    public object Clone()
    {
        var clone = new Polygon
        {
            Id = Guid.NewGuid(),
            Name = Name,
            StrokeColor = StrokeColor,
            FillColor = FillColor,
            StrokeThickness = StrokeThickness,
            IsSelected = false,
            Transform = Transform
        };
        clone.Vertices.AddRange(Vertices);
        return clone;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append($"Polygon ({Vertices.Count} vertices) ");
        if (Vertices.Count > 0 && Vertices.Count <= 4)
        {
            sb.Append("Vertices=");
            for (int i = 0; i < Vertices.Count; i++)
            {
                sb.Append($"({Vertices[i].X:F1},{Vertices[i].Y:F1})");
                if (i < Vertices.Count - 1) sb.Append(", ");
            }
        }
        return sb.ToString();
    }
}
