using GraphicsEditor.Core.Geometry;

namespace GraphicsEditor.Core.Models;

/// <summary>
/// Represents a rectangle defined by position and size.
/// </summary>
public class Rectangle : IShape
{
    public Guid Id { get; private set; }
    public string Name { get; set; }
    public uint StrokeColor { get; set; }
    public uint? FillColor { get; set; }
    public double StrokeThickness { get; set; }
    public bool IsSelected { get; set; }

    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public Rectangle()
    {
        Id = Guid.NewGuid();
        Name = "Rectangle";
        StrokeColor = 0xFF000000; // Black
        StrokeThickness = 1.0;
    }

    public Rectangle(double x, double y, double width, double height) : this()
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public bool HitTest(double x, double y, double tolerance = 5.0)
    {
        // Check if point is inside the rectangle
        if (x >= X && x <= X + Width && y >= Y && y <= Y + Height)
        {
            // If filled, any point inside counts
            if (FillColor.HasValue)
                return true;

            // For non-filled rectangles, check if point is near the outline
            bool nearLeft = Math.Abs(x - X) <= tolerance && y >= Y && y <= Y + Height;
            bool nearRight = Math.Abs(x - (X + Width)) <= tolerance && y >= Y && y <= Y + Height;
            bool nearTop = Math.Abs(y - Y) <= tolerance && x >= X && x <= X + Width;
            bool nearBottom = Math.Abs(y - (Y + Height)) <= tolerance && x >= X && x <= X + Width;

            return nearLeft || nearRight || nearTop || nearBottom;
        }

        return false;
    }

    public void Move(double deltaX, double deltaY)
    {
        X += deltaX;
        Y += deltaY;
    }

    /// <summary>
    /// Resizes the rectangle from a specific handle.
    /// </summary>
    public void Resize(double newWidth, double newHeight)
    {
        Width = Math.Max(1, newWidth);
        Height = Math.Max(1, newHeight);
    }

    public (double X, double Y, double Width, double Height) GetBounds()
    {
        return (X, Y, Width, Height);
    }

    public object Clone()
    {
        return new Rectangle
        {
            Id = Guid.NewGuid(),
            Name = Name,
            StrokeColor = StrokeColor,
            FillColor = FillColor,
            StrokeThickness = StrokeThickness,
            IsSelected = false,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height
        };
    }

    public override string ToString()
    {
        return $"{Name}: ({X:F2}, {Y:F2}), {Width:F2}x{Height:F2}";
    }
}
