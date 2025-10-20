using GraphicsEditor.Core.Models;
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
}
