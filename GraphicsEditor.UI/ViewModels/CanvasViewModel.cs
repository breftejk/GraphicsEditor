using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraphicsEditor.Core.Models;
using GraphicsEditor.Core.Geometry;
using GraphicsEditor.Core.Serialization;
using GraphicsEditor.Logic;

namespace GraphicsEditor.UI.ViewModels;

/// <summary>
/// View model for the 2D canvas editor.
/// </summary>
public partial class CanvasViewModel : ViewModelBase
{
    private readonly ShapeService _shapeService;
    private readonly ShapeManipulationService _manipulationService;

    // Public accessor for the shape service
    public ShapeService ShapeService => _shapeService;
    
    // Public accessor for the manipulation service
    public ShapeManipulationService ManipulationService => _manipulationService;

    [ObservableProperty]
    private ObservableCollection<IShape> _shapes;

    [ObservableProperty]
    private DrawingTool _selectedTool;

    [ObservableProperty]
    private IShape? _selectedShape;

    [ObservableProperty]
    private uint _currentStrokeColor = 0xFF000000; // Black

    [ObservableProperty]
    private uint? _currentFillColor = null;

    [ObservableProperty]
    private double _currentStrokeThickness = 1.0;

    // Parameters for manual shape creation
    [ObservableProperty]
    private string _lineX1 = "50";

    [ObservableProperty]
    private string _lineY1 = "50";

    [ObservableProperty]
    private string _lineX2 = "200";

    [ObservableProperty]
    private string _lineY2 = "200";

    [ObservableProperty]
    private string _rectX = "100";

    [ObservableProperty]
    private string _rectY = "100";

    [ObservableProperty]
    private string _rectWidth = "150";

    [ObservableProperty]
    private string _rectHeight = "100";

    [ObservableProperty]
    private string _circleX = "200";

    [ObservableProperty]
    private string _circleY = "200";

    [ObservableProperty]
    private string _circleRadius = "75";

    [ObservableProperty]
    private string _statusMessage = "Ready";
    
    [ObservableProperty]
    private bool _hasFill = false;

    // Bezier parameters
    [ObservableProperty]
    private int _bezierDegree = 3; // default cubic

    [ObservableProperty]
    private string _bezierPointsText = "100,100; 150,50; 250,150; 300,100";

    // Polygon parameters
    [ObservableProperty]
    private string _polygonVerticesText = "100,100; 200,100; 200,200; 100,200";

    // Transformation parameters
    [ObservableProperty]
    private string _translateX = "0";

    [ObservableProperty]
    private string _translateY = "0";

    [ObservableProperty]
    private string _pivotX = "0";

    [ObservableProperty]
    private string _pivotY = "0";

    [ObservableProperty]
    private string _rotationAngle = "45"; // degrees

    [ObservableProperty]
    private string _scaleFactor = "1.5";

    // Mouse interaction state
    private Point2D? _drawingStartPoint;
    private IShape? _tempShape;
    private bool _isDragging;
    private Point2D _dragStartPoint;

    // Selected control point index for dragging, -1 if moving whole curve
    private int _activeControlPointIndex = -1;

    public CanvasViewModel()
    {
        _shapeService = new ShapeService();
        _manipulationService = new ShapeManipulationService();
        _shapes = _shapeService.Shapes;
        _selectedTool = DrawingTool.Select;
    }

    partial void OnHasFillChanged(bool value)
    {
        if (!value)
        {
            CurrentFillColor = null;
        }
        else if (CurrentFillColor == null)
        {
            CurrentFillColor = 0xFFFFFFFF; // Default to white
        }
    }

    partial void OnCurrentStrokeThicknessChanged(double value)
    {
        // If a shape is selected, update its stroke thickness immediately
        if (SelectedShape != null)
        {
            SelectedShape.StrokeThickness = value;
            OnPropertyChanged(nameof(Shapes)); // Trigger re-render
        }
    }

    partial void OnSelectedShapeChanged(IShape? value)
    {
        // Load selected shape's parameters into text fields
        if (value is Line line)
        {
            LineX1 = line.StartPoint.X.ToString("F1");
            LineY1 = line.StartPoint.Y.ToString("F1");
            LineX2 = line.EndPoint.X.ToString("F1");
            LineY2 = line.EndPoint.Y.ToString("F1");
            
            // Update color and style controls to match selected shape
            CurrentStrokeColor = line.StrokeColor;
            CurrentStrokeThickness = line.StrokeThickness;
            CurrentFillColor = null;
            HasFill = false;
        }
        else if (value is Core.Models.Rectangle rect)
        {
            RectX = rect.X.ToString("F1");
            RectY = rect.Y.ToString("F1");
            RectWidth = rect.Width.ToString("F1");
            RectHeight = rect.Height.ToString("F1");
            
            // Update color and style controls to match selected shape
            CurrentStrokeColor = rect.StrokeColor;
            CurrentStrokeThickness = rect.StrokeThickness;
            CurrentFillColor = rect.FillColor;
            HasFill = rect.FillColor.HasValue;
        }
        else if (value is Circle circle)
        {
            CircleX = circle.Center.X.ToString("F1");
            CircleY = circle.Center.Y.ToString("F1");
            CircleRadius = circle.Radius.ToString("F1");
            
            // Update color and style controls to match selected shape
            CurrentStrokeColor = circle.StrokeColor;
            CurrentStrokeThickness = circle.StrokeThickness;
            CurrentFillColor = circle.FillColor;
            HasFill = circle.FillColor.HasValue;
        }
        else if (value is BezierCurve bez)
        {
            BezierDegree = Math.Max(0, bez.Degree);
            BezierPointsText = string.Join("; ", bez.ControlPoints.Select(p => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F1},{1:F1}", p.X, p.Y)));
            CurrentStrokeColor = bez.StrokeColor;
            CurrentStrokeThickness = bez.StrokeThickness;
            CurrentFillColor = null;
            HasFill = false;
        }
        else if (value is Polygon polygon)
        {
            var vertices = polygon.GetTransformedVertices();
            PolygonVerticesText = string.Join("; ", vertices.Select(p => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F1},{1:F1}", p.X, p.Y)));
            CurrentStrokeColor = polygon.StrokeColor;
            CurrentStrokeThickness = polygon.StrokeThickness;
            CurrentFillColor = polygon.FillColor;
            HasFill = polygon.FillColor.HasValue;
            
            // Set pivot to centroid by default
            var centroid = polygon.Centroid;
            PivotX = centroid.X.ToString("F1");
            PivotY = centroid.Y.ToString("F1");
        }

        // Update pivot to center of bounds for any shape
        if (value != null)
        {
            var bounds = value.GetBounds();
            PivotX = (bounds.X + bounds.Width / 2).ToString("F1");
            PivotY = (bounds.Y + bounds.Height / 2).ToString("F1");
        }
        
        // Notify properties for visibility
        OnPropertyChanged(nameof(IsLineSelected));
        OnPropertyChanged(nameof(IsRectangleSelected));
        OnPropertyChanged(nameof(IsCircleSelected));
        OnPropertyChanged(nameof(IsBezierSelected));
        OnPropertyChanged(nameof(IsPolygonSelected));
    }
    
    // Properties to control visibility of parameter sections
    public bool IsLineSelected => SelectedShape is Line || SelectedShape == null;
    public bool IsRectangleSelected => SelectedShape is Core.Models.Rectangle || SelectedShape == null;
    public bool IsCircleSelected => SelectedShape is Circle || SelectedShape == null;
    public bool IsBezierSelected => SelectedShape is BezierCurve || SelectedShape == null;
    public bool IsPolygonSelected => SelectedShape is Polygon || SelectedShape == null;

    [RelayCommand]
    private void AddLineFromParams()
    {
        if (TryParse(LineX1, out double x1) && TryParse(LineY1, out double y1) &&
            TryParse(LineX2, out double x2) && TryParse(LineY2, out double y2))
        {
            var line = new Line(x1, y1, x2, y2)
            {
                StrokeColor = CurrentStrokeColor,
                StrokeThickness = CurrentStrokeThickness
            };
            _shapeService.AddShape(line);
            StatusMessage = $"Added {line}";
        }
        else
        {
            StatusMessage = "Invalid line parameters";
        }
    }

    [RelayCommand]
    private void AddRectangleFromParams()
    {
        if (TryParse(RectX, out double x) && TryParse(RectY, out double y) &&
            TryParse(RectWidth, out double width) && TryParse(RectHeight, out double height))
        {
            var rect = new Core.Models.Rectangle(x, y, width, height)
            {
                StrokeColor = CurrentStrokeColor,
                FillColor = CurrentFillColor,
                StrokeThickness = CurrentStrokeThickness
            };
            _shapeService.AddShape(rect);
            StatusMessage = $"Added {rect}";
        }
        else
        {
            StatusMessage = "Invalid rectangle parameters";
        }
    }

    [RelayCommand]
    private void AddCircleFromParams()
    {
        if (TryParse(CircleX, out double x) && TryParse(CircleY, out double y) &&
            TryParse(CircleRadius, out double radius))
        {
            var circle = new Circle(x, y, radius)
            {
                StrokeColor = CurrentStrokeColor,
                FillColor = CurrentFillColor,
                StrokeThickness = CurrentStrokeThickness
            };
            _shapeService.AddShape(circle);
            StatusMessage = $"Added {circle}";
        }
        else
        {
            StatusMessage = "Invalid circle parameters";
        }
    }

    [RelayCommand]
    private void AddBezierFromParams()
    {
        var points = ParseBezierPoints(BezierPointsText);
        int required = BezierDegree + 1;
        if (points.Count != required)
        {
            StatusMessage = $"Bezier requires exactly {required} control points for degree {BezierDegree}";
            return;
        }
        var curve = new BezierCurve(points)
        {
            StrokeColor = CurrentStrokeColor,
            StrokeThickness = CurrentStrokeThickness
        };
        _shapeService.AddShape(curve);
        StatusMessage = $"Added Bezier deg {curve.Degree} with {curve.ControlPoints.Count} pts";
    }

    [RelayCommand]
    private void AddPolygonFromParams()
    {
        var vertices = ParsePolygonVertices(PolygonVerticesText);
        if (vertices.Count < 3)
        {
            StatusMessage = "Polygon requires at least 3 vertices";
            return;
        }
        var polygon = new Polygon(vertices)
        {
            StrokeColor = CurrentStrokeColor,
            FillColor = CurrentFillColor,
            StrokeThickness = CurrentStrokeThickness
        };
        _shapeService.AddShape(polygon);
        StatusMessage = $"Added {polygon}";
    }

    [RelayCommand]
    private void ApplyTranslation()
    {
        if (SelectedShape == null)
        {
            StatusMessage = "No shape selected";
            return;
        }

        if (TryParse(TranslateX, out double tx) && TryParse(TranslateY, out double ty))
        {
            _shapeService.TranslateSelectedShape(tx, ty);
            UpdateParametersFromSelectedShape();
            OnPropertyChanged(nameof(Shapes));
            StatusMessage = $"Translated by ({tx:F1}, {ty:F1})";
        }
        else
        {
            StatusMessage = "Invalid translation values";
        }
    }

    [RelayCommand]
    private void ApplyRotation()
    {
        if (SelectedShape == null)
        {
            StatusMessage = "No shape selected";
            return;
        }

        if (TryParse(PivotX, out double px) && TryParse(PivotY, out double py) &&
            TryParse(RotationAngle, out double angle))
        {
            var pivot = new Point2D(px, py);
            double angleRadians = angle * Math.PI / 180.0; // Convert degrees to radians
            _shapeService.RotateSelectedShape(pivot, angleRadians);
            UpdateParametersFromSelectedShape();
            OnPropertyChanged(nameof(Shapes));
            StatusMessage = $"Rotated {angle:F1}° around ({px:F1}, {py:F1})";
        }
        else
        {
            StatusMessage = "Invalid rotation values";
        }
    }

    [RelayCommand]
    private void ApplyScale()
    {
        if (SelectedShape == null)
        {
            StatusMessage = "No shape selected";
            return;
        }

        if (TryParse(PivotX, out double px) && TryParse(PivotY, out double py) &&
            TryParse(ScaleFactor, out double scale))
        {
            if (scale <= 0)
            {
                StatusMessage = "Scale factor must be positive";
                return;
            }
            var pivot = new Point2D(px, py);
            _shapeService.ScaleSelectedShape(pivot, scale);
            UpdateParametersFromSelectedShape();
            OnPropertyChanged(nameof(Shapes));
            StatusMessage = $"Scaled by {scale:F2}x around ({px:F1}, {py:F1})";
        }
        else
        {
            StatusMessage = "Invalid scale values";
        }
    }

    [RelayCommand]
    private void SetPivotToCenter()
    {
        if (SelectedShape == null)
        {
            StatusMessage = "No shape selected";
            return;
        }

        var bounds = SelectedShape.GetBounds();
        PivotX = (bounds.X + bounds.Width / 2).ToString("F1");
        PivotY = (bounds.Y + bounds.Height / 2).ToString("F1");
        StatusMessage = "Pivot set to shape center";
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedShape != null && _shapeService.RemoveSelectedShape())
        {
            SelectedShape = null;
            StatusMessage = "Shape deleted";
            OnPropertyChanged(nameof(Shapes)); // Trigger UI update
        }
        else
        {
            StatusMessage = "No shape selected to delete";
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        _shapeService.ClearShapes();
        SelectedShape = null;
        StatusMessage = "Canvas cleared";
    }

    [RelayCommand]
    private async Task SaveShapesAsync()
    {
        try
        {
            string filePath = "shapes.json"; // In a real app, use a file dialog
            await ShapeSerializer.SerializeToFileAsync(Shapes, filePath);
            StatusMessage = $"Shapes saved to {filePath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadShapesAsync()
    {
        try
        {
            string filePath = "shapes.json"; // In a real app, use a file dialog
            if (File.Exists(filePath))
            {
                var shapes = await ShapeSerializer.DeserializeFromFileAsync(filePath);
                _shapeService.LoadShapes(shapes);
                StatusMessage = $"Shapes loaded from {filePath}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SetStrokeColor(object? parameter)
    {
        if (parameter is string hex && uint.TryParse(hex.TrimStart('#', '0', 'x'), System.Globalization.NumberStyles.HexNumber, null, out uint color))
        {
            CurrentStrokeColor = color;
            
            // If a shape is selected, update its stroke color immediately
            if (SelectedShape != null)
            {
                SelectedShape.StrokeColor = color;
                OnPropertyChanged(nameof(Shapes)); // Trigger re-render
                StatusMessage = $"Updated selected shape stroke color to #{color:X8}";
            }
            else
            {
                StatusMessage = $"Stroke color set to #{color:X8}";
            }
        }
    }

    [RelayCommand]
    private void SetFillColor(object? parameter)
    {
        if (parameter is string hex && uint.TryParse(hex.TrimStart('#', '0', 'x'), System.Globalization.NumberStyles.HexNumber, null, out uint color))
        {
            CurrentFillColor = color;
            HasFill = true;
            
            // If a shape is selected, update its fill color immediately
            if (SelectedShape != null)
            {
                if (SelectedShape is Core.Models.Rectangle rect)
                {
                    rect.FillColor = color;
                }
                else if (SelectedShape is Circle circle)
                {
                    circle.FillColor = color;
                }
                OnPropertyChanged(nameof(Shapes)); // Trigger re-render
                StatusMessage = $"Updated selected shape fill color to #{color:X8}";
            }
            else
            {
                StatusMessage = $"Fill color set to #{color:X8}";
            }
        }
    }

    [RelayCommand]
    private void UpdateSelectedShape()
    {
        if (SelectedShape == null)
        {
            StatusMessage = "No shape selected";
            return;
        }

        try
        {
            if (SelectedShape is Line line)
            {
                if (TryParse(LineX1, out double x1) && TryParse(LineY1, out double y1) &&
                    TryParse(LineX2, out double x2) && TryParse(LineY2, out double y2))
                {
                    line.StartPoint = new Point2D(x1, y1);
                    line.EndPoint = new Point2D(x2, y2);
                    OnPropertyChanged(nameof(Shapes)); // Trigger re-render
                    StatusMessage = "Line updated";
                }
                else
                {
                    StatusMessage = "Invalid line parameters";
                    return;
                }
            }
            else if (SelectedShape is Core.Models.Rectangle rect)
            {
                if (TryParse(RectX, out double x) && TryParse(RectY, out double y) &&
                    TryParse(RectWidth, out double width) && TryParse(RectHeight, out double height))
                {
                    rect.X = x;
                    rect.Y = y;
                    rect.Width = width;
                    rect.Height = height;
                    OnPropertyChanged(nameof(Shapes)); // Trigger re-render
                    StatusMessage = "Rectangle updated";
                }
                else
                {
                    StatusMessage = "Invalid rectangle parameters";
                    return;
                }
            }
            else if (SelectedShape is Circle circle)
            {
                if (TryParse(CircleX, out double x) && TryParse(CircleY, out double y) &&
                    TryParse(CircleRadius, out double radius))
                {
                    circle.Center = new Point2D(x, y);
                    circle.Radius = radius;
                    OnPropertyChanged(nameof(Shapes)); // Trigger re-render
                    StatusMessage = "Circle updated";
                }
                else
                {
                    StatusMessage = "Invalid circle parameters";
                    return;
                }
            }
            else if (SelectedShape is BezierCurve bez)
            {
                var pts = ParseBezierPoints(BezierPointsText);
                if (pts.Count < 2)
                {
                    StatusMessage = "Invalid bezier points";
                    return;
                }
                bez.ControlPoints.Clear();
                foreach (var p in pts) bez.ControlPoints.Add(p);
                OnPropertyChanged(nameof(Shapes));
                StatusMessage = "Bezier updated";
            }
            else if (SelectedShape is Polygon polygon)
            {
                var vertices = ParsePolygonVertices(PolygonVerticesText);
                if (vertices.Count < 3)
                {
                    StatusMessage = "Polygon requires at least 3 vertices";
                    return;
                }
                polygon.Vertices.Clear();
                foreach (var v in vertices) polygon.Vertices.Add(v);
                polygon.ResetTransform(); // Reset transform since we're setting new vertices
                OnPropertyChanged(nameof(Shapes));
                StatusMessage = "Polygon updated";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error updating shape: {ex.Message}";
        }
    }

    /// <summary>
    /// Handles mouse down event on canvas.
    /// </summary>
    public void OnMouseDown(double x, double y)
    {
        var point = new Point2D(x, y);

        if (SelectedTool == DrawingTool.Select)
        {
            if (_shapeService.SelectShapeAt(x, y))
            {
                SelectedShape = _shapeService.SelectedShape;
                _isDragging = true;
                _dragStartPoint = point;
                StatusMessage = $"Selected: {SelectedShape}";
            }
            else
            {
                _shapeService.DeselectShape();
                SelectedShape = null;
                StatusMessage = "Selection cleared";
            }
        }
        else if (SelectedTool == DrawingTool.Bezier)
        {
            // Start new curve if none is selected while drawing
            if (_tempShape is not BezierCurve temp)
            {
                temp = new BezierCurve();
                temp.StrokeColor = CurrentStrokeColor;
                temp.StrokeThickness = CurrentStrokeThickness;
                _tempShape = temp;
                _shapeService.AddShape(temp);
            }
            ((BezierCurve)_tempShape).AddPoint(point);
            StatusMessage = $"Added control point ({x:F1},{y:F1})";
            return;
        }
        else if (SelectedTool == DrawingTool.Polygon)
        {
            // Start new polygon if none is being constructed
            if (_tempShape is not Polygon tempPolygon)
            {
                tempPolygon = new Polygon();
                tempPolygon.StrokeColor = CurrentStrokeColor;
                tempPolygon.FillColor = CurrentFillColor;
                tempPolygon.StrokeThickness = CurrentStrokeThickness;
                _tempShape = tempPolygon;
                _shapeService.AddShape(tempPolygon);
            }
            ((Polygon)_tempShape).AddVertex(point);
            StatusMessage = $"Added vertex ({x:F1},{y:F1}) - Click more or 'Finish Polygon' when done";
            return;
        }
        else
        {
            _drawingStartPoint = point;
        }
    }

    /// <summary>
    /// Handles mouse move event on canvas.
    /// </summary>
    public void OnMouseMove(double x, double y)
    {
        var point = new Point2D(x, y);

        if (SelectedTool == DrawingTool.Bezier && _tempShape is BezierCurve temp)
        {
            // Real-time preview by updating last point if mouse is moving while dragging
            if (temp.ControlPoints.Count > 0)
            {
                temp.ControlPoints[temp.ControlPoints.Count - 1] = point;
                OnPropertyChanged(nameof(Shapes));
            }
            return;
        }

        // Drag control point if selected
        if (_isDragging && SelectedShape is BezierCurve bez && _activeControlPointIndex >= 0)
        {
            bez.ControlPoints[_activeControlPointIndex] = point;
            _dragStartPoint = point;
            OnPropertyChanged(nameof(Shapes));
            return;
        }

        if (_isDragging && SelectedShape != null && SelectedTool == DrawingTool.Select)
        {
            double deltaX = point.X - _dragStartPoint.X;
            double deltaY = point.Y - _dragStartPoint.Y;
            _shapeService.MoveSelectedShape(deltaX, deltaY);
            _dragStartPoint = point;
        }
        else if (_drawingStartPoint.HasValue)
        {
            // Update temporary shape preview
            UpdateTempShape(_drawingStartPoint.Value, point);
        }
    }

    /// <summary>
    /// Handles mouse up event on canvas.
    /// </summary>
    public void OnMouseUp(double x, double y)
    {
        var point = new Point2D(x, y);

        if (SelectedTool == DrawingTool.Bezier)
        {
            // Keep the newly added point; user keeps clicking to add more. End with right click or tool change.
            return;
        }

        if (SelectedTool == DrawingTool.Polygon)
        {
            // Keep the newly added vertex; user keeps clicking to add more. End with FinishPolygon command.
            return;
        }

        if (_isDragging)
        {
            _isDragging = false;
            // Update parameter fields after moving the shape
            UpdateParametersFromSelectedShape();
            StatusMessage = "Shape moved";
        }
        else if (_drawingStartPoint.HasValue)
        {
            CreateShapeFromPoints(_drawingStartPoint.Value, point);
            _drawingStartPoint = null;
            _tempShape = null;
        }
    }

    private void UpdateTempShape(Point2D start, Point2D end)
    {
        // This would be used to show a preview while drawing
        // Implementation depends on UI rendering approach
    }

    private void CreateShapeFromPoints(Point2D start, Point2D end)
    {
        IShape? shape = SelectedTool switch
        {
            DrawingTool.Line => new Line(start, end),
            DrawingTool.Rectangle => new Core.Models.Rectangle(
                Math.Min(start.X, end.X),
                Math.Min(start.Y, end.Y),
                Math.Abs(end.X - start.X),
                Math.Abs(end.Y - start.Y)),
            DrawingTool.Circle => new Circle(
                start,
                start.DistanceTo(end)),
            _ => null
        };

        if (shape != null)
        {
            shape.StrokeColor = CurrentStrokeColor;
            shape.FillColor = CurrentFillColor;
            shape.StrokeThickness = CurrentStrokeThickness;
            _shapeService.AddShape(shape);
            StatusMessage = $"Created {shape}";
        }
    }

    private bool TryParse(string value, out double result)
    {
        return double.TryParse(value, out result);
    }

    /// <summary>
    /// Updates parameter text fields from the currently selected shape.
    /// </summary>
    public void UpdateParametersFromSelectedShape()
    {
        if (SelectedShape == null) return;

        if (SelectedShape is Line line)
        {
            LineX1 = line.StartPoint.X.ToString("F1");
            LineY1 = line.StartPoint.Y.ToString("F1");
            LineX2 = line.EndPoint.X.ToString("F1");
            LineY2 = line.EndPoint.Y.ToString("F1");
        }
        else if (SelectedShape is Core.Models.Rectangle rect)
        {
            RectX = rect.X.ToString("F1");
            RectY = rect.Y.ToString("F1");
            RectWidth = rect.Width.ToString("F1");
            RectHeight = rect.Height.ToString("F1");
        }
        else if (SelectedShape is Circle circle)
        {
            CircleX = circle.Center.X.ToString("F1");
            CircleY = circle.Center.Y.ToString("F1");
            CircleRadius = circle.Radius.ToString("F1");
        }
        else if (SelectedShape is BezierCurve bez)
        {
            BezierDegree = Math.Max(0, bez.Degree);
            BezierPointsText = string.Join("; ", bez.ControlPoints.Select(p => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F1},{1:F1}", p.X, p.Y)));
        }
        else if (SelectedShape is Polygon polygon)
        {
            var vertices = polygon.GetTransformedVertices();
            PolygonVerticesText = string.Join("; ", vertices.Select(p => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F1},{1:F1}", p.X, p.Y)));
        }

        // Update pivot to center of bounds
        if (SelectedShape != null)
        {
            var bounds = SelectedShape.GetBounds();
            PivotX = (bounds.X + bounds.Width / 2).ToString("F1");
            PivotY = (bounds.Y + bounds.Height / 2).ToString("F1");
        }
    }

    [RelayCommand]
    private void FinishBezier()
    {
        if (_tempShape is BezierCurve temp)
        {
            int required = BezierDegree + 1;
            if (temp.ControlPoints.Count == required)
            {
                _tempShape = null;
                StatusMessage = $"Bezier created with {temp.ControlPoints.Count} points (deg {temp.Degree})";
            }
            else
            {
                StatusMessage = $"Bezier requires exactly {required} control points (currently {temp.ControlPoints.Count})";
            }
        }
        else
        {
            StatusMessage = "No active Bezier to finish";
        }
    }

    private List<Point2D> ParseBezierPoints(string text)
    {
        var result = new List<Point2D>();
        if (string.IsNullOrWhiteSpace(text)) return result;
        var parts = text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var xy = part.Trim().Split(',');
            if (xy.Length == 2 
                && double.TryParse(xy[0], System.Globalization.NumberFormatInfo.InvariantInfo, out double x) 
                && double.TryParse(xy[1], System.Globalization.NumberFormatInfo.InvariantInfo, out double y))
            {
                result.Add(new Point2D(x, y));
            }
        }
        return result;
    }

    private List<Point2D> ParsePolygonVertices(string text)
    {
        var result = new List<Point2D>();
        if (string.IsNullOrWhiteSpace(text)) return result;
        var parts = text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var xy = part.Trim().Split(',');
            if (xy.Length == 2 
                && double.TryParse(xy[0], System.Globalization.NumberFormatInfo.InvariantInfo, out double x) 
                && double.TryParse(xy[1], System.Globalization.NumberFormatInfo.InvariantInfo, out double y))
            {
                result.Add(new Point2D(x, y));
            }
        }
        return result;
    }

    // Event to request finishing polygon from view
    public event Action? FinishPolygonRequested;

    [RelayCommand]
    private void FinishPolygon()
    {
        if (_tempShape is Polygon temp)
        {
            if (temp.Vertices.Count >= 3)
            {
                _tempShape = null;
                StatusMessage = $"Polygon created with {temp.VertexCount} vertices";
                SelectedShape = temp;
            }
            else
            {
                StatusMessage = $"Polygon requires at least 3 vertices (currently {temp.VertexCount})";
            }
        }
        else
        {
            StatusMessage = "No active Polygon to finish";
        }
        FinishPolygonRequested?.Invoke();
    }
}

public enum DrawingTool
{
    Select,
    Line,
    Rectangle,
    Circle,
    Bezier,
    Polygon
}
