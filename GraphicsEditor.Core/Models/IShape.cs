using System.Text.Json.Serialization;

namespace GraphicsEditor.Core.Models;

/// <summary>
/// Base interface for all drawable shapes in the graphics editor.
/// Provides core functionality for shape manipulation, serialization, and rendering.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")] // ensure $type is emitted and read
[JsonDerivedType(typeof(Line), typeDiscriminator: "line")]
[JsonDerivedType(typeof(Rectangle), typeDiscriminator: "rectangle")]
[JsonDerivedType(typeof(Circle), typeDiscriminator: "circle")]
[JsonDerivedType(typeof(BezierCurve), typeDiscriminator: "bezier")] // added
public interface IShape : ICloneable
{
    /// <summary>
    /// Unique identifier for the shape.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Name or label for the shape.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Stroke color of the shape (ARGB format).
    /// </summary>
    uint StrokeColor { get; set; }

    /// <summary>
    /// Fill color of the shape (ARGB format). Null for no fill.
    /// </summary>
    uint? FillColor { get; set; }

    /// <summary>
    /// Stroke thickness in pixels.
    /// </summary>
    double StrokeThickness { get; set; }

    /// <summary>
    /// Indicates whether the shape is currently selected.
    /// </summary>
    bool IsSelected { get; set; }

    /// <summary>
    /// Checks if a point is within the shape's bounds or on its outline.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="tolerance">Distance tolerance for hit detection</param>
    /// <returns>True if the point hits the shape</returns>
    bool HitTest(double x, double y, double tolerance = 5.0);

    /// <summary>
    /// Moves the shape by the specified offset.
    /// </summary>
    /// <param name="deltaX">Horizontal offset</param>
    /// <param name="deltaY">Vertical offset</param>
    void Move(double deltaX, double deltaY);

    /// <summary>
    /// Gets the bounding box of the shape.
    /// </summary>
    /// <returns>Bounding rectangle (X, Y, Width, Height)</returns>
    (double X, double Y, double Width, double Height) GetBounds();
}
