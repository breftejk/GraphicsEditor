using GraphicsEditor.Core.Models;
using GraphicsEditor.Core.Geometry;
using System.Collections.ObjectModel;

namespace GraphicsEditor.Logic;

/// <summary>
/// Manages shape collections, selection, and basic shape operations.
/// </summary>
public class ShapeService
{
    private readonly ObservableCollection<IShape> _shapes;
    private IShape? _selectedShape;

    public ObservableCollection<IShape> Shapes => _shapes;
    public IShape? SelectedShape
    {
        get => _selectedShape;
        private set
        {
            if (_selectedShape != null)
                _selectedShape.IsSelected = false;

            _selectedShape = value;

            if (_selectedShape != null)
                _selectedShape.IsSelected = true;
        }
    }

    public ShapeService()
    {
        _shapes = new ObservableCollection<IShape>();
    }

    /// <summary>
    /// Adds a shape to the collection.
    /// </summary>
    public void AddShape(IShape shape)
    {
        _shapes.Add(shape);
    }

    /// <summary>
    /// Removes a shape from the collection.
    /// </summary>
    public bool RemoveShape(IShape shape)
    {
        if (SelectedShape == shape)
            SelectedShape = null;

        return _shapes.Remove(shape);
    }

    /// <summary>
    /// Removes the currently selected shape.
    /// </summary>
    public bool RemoveSelectedShape()
    {
        if (SelectedShape == null)
            return false;

        return RemoveShape(SelectedShape);
    }

    /// <summary>
    /// Clears all shapes from the collection.
    /// </summary>
    public void ClearShapes()
    {
        SelectedShape = null;
        _shapes.Clear();
    }

    /// <summary>
    /// Performs hit testing to find a shape at the specified coordinates.
    /// </summary>
    public IShape? HitTest(double x, double y, double tolerance = 5.0)
    {
        // Search in reverse order (topmost shapes first)
        for (int i = _shapes.Count - 1; i >= 0; i--)
        {
            if (_shapes[i].HitTest(x, y, tolerance))
                return _shapes[i];
        }

        return null;
    }

    /// <summary>
    /// Selects a shape at the specified coordinates.
    /// </summary>
    public bool SelectShapeAt(double x, double y, double tolerance = 5.0)
    {
        var shape = HitTest(x, y, tolerance);
        SelectedShape = shape;
        return shape != null;
    }

    /// <summary>
    /// Deselects the currently selected shape.
    /// </summary>
    public void DeselectShape()
    {
        SelectedShape = null;
    }

    /// <summary>
    /// Duplicates the currently selected shape.
    /// </summary>
    public IShape? DuplicateSelectedShape()
    {
        if (SelectedShape == null)
            return null;

        var duplicate = (IShape)SelectedShape.Clone();
        duplicate.Move(10, 10); // Offset the duplicate slightly
        AddShape(duplicate);
        SelectedShape = duplicate;

        return duplicate;
    }

    /// <summary>
    /// Moves the selected shape by the specified offset.
    /// </summary>
    public void MoveSelectedShape(double deltaX, double deltaY)
    {
        SelectedShape?.Move(deltaX, deltaY);
    }

    /// <summary>
    /// Loads shapes from a collection.
    /// </summary>
    public void LoadShapes(IEnumerable<IShape> shapes)
    {
        ClearShapes();
        foreach (var shape in shapes)
        {
            AddShape(shape);
        }
    }

    /// <summary>
    /// Translates the selected shape by a vector using homogeneous coordinates.
    /// </summary>
    public void TranslateSelectedShape(double deltaX, double deltaY)
    {
        if (SelectedShape == null) return;

        if (SelectedShape is Polygon polygon)
        {
            var translation = Matrix3x3.CreateTranslation(deltaX, deltaY);
            polygon.ApplyTransform(translation);
        }
        else
        {
            // For non-polygon shapes, use the existing Move method
            SelectedShape.Move(deltaX, deltaY);
        }
    }

    /// <summary>
    /// Rotates the selected shape around a pivot point using homogeneous coordinates.
    /// </summary>
    /// <param name="pivot">The point to rotate around</param>
    /// <param name="angleRadians">Rotation angle in radians</param>
    public void RotateSelectedShape(Point2D pivot, double angleRadians)
    {
        if (SelectedShape == null) return;

        if (SelectedShape is Polygon polygon)
        {
            polygon.Rotate(pivot, angleRadians);
        }
        else if (SelectedShape is Line line)
        {
            var rotation = Matrix3x3.CreateRotationAround(pivot, angleRadians);
            line.StartPoint = rotation.Transform(line.StartPoint);
            line.EndPoint = rotation.Transform(line.EndPoint);
        }
        else if (SelectedShape is Rectangle rect)
        {
            // For rectangles, rotate the four corners and create new bounds
            var rotation = Matrix3x3.CreateRotationAround(pivot, angleRadians);
            var corners = new Point2D[4]
            {
                new Point2D(rect.X, rect.Y),
                new Point2D(rect.X + rect.Width, rect.Y),
                new Point2D(rect.X, rect.Y + rect.Height),
                new Point2D(rect.X + rect.Width, rect.Y + rect.Height)
            };
            var rotated = new Point2D[corners.Length];
            for (int i = 0; i < corners.Length; i++)
            {
                rotated[i] = rotation.Transform(corners[i]);
            }
            double minX = rotated.Min(p => p.X);
            double minY = rotated.Min(p => p.Y);
            double maxX = rotated.Max(p => p.X);
            double maxY = rotated.Max(p => p.Y);
            rect.X = minX;
            rect.Y = minY;
            rect.Width = maxX - minX;
            rect.Height = maxY - minY;
        }
        else if (SelectedShape is Circle circle)
        {
            // For circles, just rotate the center
            var rotation = Matrix3x3.CreateRotationAround(pivot, angleRadians);
            circle.Center = rotation.Transform(circle.Center);
        }
        else if (SelectedShape is BezierCurve bezier)
        {
            var rotation = Matrix3x3.CreateRotationAround(pivot, angleRadians);
            for (int i = 0; i < bezier.ControlPoints.Count; i++)
            {
                bezier.ControlPoints[i] = rotation.Transform(bezier.ControlPoints[i]);
            }
        }
    }

    /// <summary>
    /// Scales the selected shape around a pivot point using homogeneous coordinates.
    /// </summary>
    /// <param name="pivot">The point to scale around</param>
    /// <param name="scaleFactor">Scale factor (1.0 = no change)</param>
    public void ScaleSelectedShape(Point2D pivot, double scaleFactor)
    {
        ScaleSelectedShape(pivot, scaleFactor, scaleFactor);
    }

    /// <summary>
    /// Scales the selected shape non-uniformly around a pivot point using homogeneous coordinates.
    /// </summary>
    /// <param name="pivot">The point to scale around</param>
    /// <param name="scaleX">Scale factor in X direction</param>
    /// <param name="scaleY">Scale factor in Y direction</param>
    public void ScaleSelectedShape(Point2D pivot, double scaleX, double scaleY)
    {
        if (SelectedShape == null) return;

        if (SelectedShape is Polygon polygon)
        {
            polygon.Scale(pivot, scaleX, scaleY);
        }
        else if (SelectedShape is Line line)
        {
            var scaling = Matrix3x3.CreateScaleAround(pivot, scaleX, scaleY);
            line.StartPoint = scaling.Transform(line.StartPoint);
            line.EndPoint = scaling.Transform(line.EndPoint);
        }
        else if (SelectedShape is Rectangle rect)
        {
            var scaling = Matrix3x3.CreateScaleAround(pivot, scaleX, scaleY);
            var topLeft = scaling.Transform(new Point2D(rect.X, rect.Y));
            var bottomRight = scaling.Transform(new Point2D(rect.X + rect.Width, rect.Y + rect.Height));
            rect.X = Math.Min(topLeft.X, bottomRight.X);
            rect.Y = Math.Min(topLeft.Y, bottomRight.Y);
            rect.Width = Math.Abs(bottomRight.X - topLeft.X);
            rect.Height = Math.Abs(bottomRight.Y - topLeft.Y);
        }
        else if (SelectedShape is Circle circle)
        {
            var scaling = Matrix3x3.CreateScaleAround(pivot, scaleX, scaleY);
            circle.Center = scaling.Transform(circle.Center);
            // For uniform scaling, adjust radius
            circle.Radius *= Math.Abs(scaleX);
        }
        else if (SelectedShape is BezierCurve bezier)
        {
            var scaling = Matrix3x3.CreateScaleAround(pivot, scaleX, scaleY);
            for (int i = 0; i < bezier.ControlPoints.Count; i++)
            {
                bezier.ControlPoints[i] = scaling.Transform(bezier.ControlPoints[i]);
            }
        }
    }

    /// <summary>
    /// Selects a specific shape.
    /// </summary>
    public void SelectShape(IShape? shape)
    {
        SelectedShape = shape;
    }
}
