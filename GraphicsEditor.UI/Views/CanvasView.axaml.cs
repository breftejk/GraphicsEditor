using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using GraphicsEditor.UI.ViewModels;
using GraphicsEditor.Core.Models;
using GraphicsEditor.Core.Geometry;
using System;
using System.Linq;

namespace GraphicsEditor.UI.Views;

public partial class CanvasView : UserControl
{
    private CanvasViewModel? ViewModel => DataContext as CanvasViewModel;
    private Canvas? _drawingCanvas;
    private bool _isDrawing = false;
    private bool _isDragging = false;
    private bool _isResizing = false;
    private ResizeHandle _activeHandle = ResizeHandle.None;
    private Avalonia.Point _startPoint;
    private Avalonia.Point _lastDragPoint;
    private IShape? _selectedShape;
    private const double HandleSize = 8.0;
    private const double VertexHandleRadius = 4.0;
    private const double VertexSelectionTolerance = 6.0;

    private BezierCurve? _activeBezier; // currently constructed curve when tool=Bezier
    private int _activeBezierControlPointIndex = -1; // index of CP being dragged
    private Core.Models.Polygon? _activePolygon; // currently constructed polygon when tool=Polygon
    private int _activePolygonVertexIndex = -1; // index of vertex being dragged

    public CanvasView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.ShapeService.Shapes.CollectionChanged += (s, e) => RenderShapes();
            ViewModel.PropertyChanged += (s, e) => 
            {
                // Re-render when Shapes property changes (triggered by color/thickness updates)
                if (e.PropertyName == nameof(ViewModel.Shapes))
                {
                    RenderShapes();
                }
                
                // Reset active Bezier construction when changing tool away from Bezier
                if (e.PropertyName == nameof(ViewModel.SelectedTool) && ViewModel.SelectedTool != DrawingTool.Bezier)
                {
                    _activeBezier = null;
                }

                // Reset active Polygon construction when changing tool away from Polygon
                if (e.PropertyName == nameof(ViewModel.SelectedTool) && ViewModel.SelectedTool != DrawingTool.Polygon)
                {
                    _activePolygon = null;
                }
                
                // Sync selected shape when ViewModel's SelectedShape changes
                if (e.PropertyName == nameof(ViewModel.SelectedShape))
                {
                    // Clear old selection
                    if (_selectedShape != null)
                    {
                        _selectedShape.IsSelected = false;
                    }
                    
                    // Update to new selection
                    _selectedShape = ViewModel.SelectedShape;
                    
                    // Mark new selection
                    if (_selectedShape != null)
                    {
                        _selectedShape.IsSelected = true;
                    }
                    
                    RenderShapes();
                }
            };

            // Subscribe to FinishPolygonRequested event
            ViewModel.FinishPolygonRequested += () => { _activePolygon = null; };

            _drawingCanvas = this.FindControl<Canvas>("DrawingCanvas");
            RenderShapes();
        }
    }

    private void DrawingCanvas_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel == null || _drawingCanvas == null) return;
        
        _startPoint = e.GetPosition(_drawingCanvas);

        // If Bezier tool is active: add a control point on click (start new curve if needed)
        if (ViewModel.SelectedTool == DrawingTool.Bezier)
        {
            if (_activeBezier == null)
            {
                _activeBezier = new BezierCurve();
                _activeBezier.StrokeColor = ViewModel.CurrentStrokeColor;
                _activeBezier.StrokeThickness = ViewModel.CurrentStrokeThickness;
                ViewModel.ShapeService.AddShape(_activeBezier);
                ViewModel.SelectedShape = _activeBezier;
            }
            _activeBezier.ControlPoints.Add(new Point2D(_startPoint.X, _startPoint.Y));
            RenderShapes();
            return;
        }

        // If Polygon tool is active: add a vertex on click (start new polygon if needed)
        if (ViewModel.SelectedTool == DrawingTool.Polygon)
        {
            if (_activePolygon == null)
            {
                _activePolygon = new Core.Models.Polygon();
                _activePolygon.StrokeColor = ViewModel.CurrentStrokeColor;
                _activePolygon.FillColor = ViewModel.CurrentFillColor;
                _activePolygon.StrokeThickness = ViewModel.CurrentStrokeThickness;
                ViewModel.ShapeService.AddShape(_activePolygon);
                ViewModel.SelectedShape = _activePolygon;
            }
            _activePolygon.AddVertex(_startPoint.X, _startPoint.Y);
            RenderShapes();
            return;
        }

        // If Select tool: prefer grabbing a Polygon vertex if near cursor
        if (ViewModel.SelectedTool == DrawingTool.Select)
        {
            for (int si = ViewModel.ShapeService.Shapes.Count - 1; si >= 0; si--)
            {
                if (ViewModel.ShapeService.Shapes[si] is Core.Models.Polygon polygon)
                {
                    var vertices = polygon.GetTransformedVertices();
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        var v = vertices[i];
                        if (Math.Abs(v.X - _startPoint.X) <= VertexSelectionTolerance && Math.Abs(v.Y - _startPoint.Y) <= VertexSelectionTolerance)
                        {
                            ViewModel.SelectedShape = polygon;
                            _selectedShape = polygon;
                            _selectedShape.IsSelected = true;
                            _activePolygonVertexIndex = i;
                            _isDragging = true;
                            _lastDragPoint = _startPoint;
                            RenderShapes();
                            return;
                        }
                    }
                }
            }
        }

        // If Select tool: prefer grabbing a Bezier control point if near cursor (even if not selected yet)
        if (ViewModel.SelectedTool == DrawingTool.Select)
        {
            for (int si = ViewModel.ShapeService.Shapes.Count - 1; si >= 0; si--)
            {
                if (ViewModel.ShapeService.Shapes[si] is BezierCurve bezier)
                {
                    for (int i = 0; i < bezier.ControlPoints.Count; i++)
                    {
                        var cp = bezier.ControlPoints[i];
                        if (Math.Abs(cp.X - _startPoint.X) <= 6 && Math.Abs(cp.Y - _startPoint.Y) <= 6)
                        {
                            ViewModel.SelectedShape = bezier;
                            _selectedShape = bezier;
                            _selectedShape.IsSelected = true;
                            _activeBezierControlPointIndex = i;
                            _isDragging = true;
                            _lastDragPoint = _startPoint;
                            RenderShapes();
                            return;
                        }
                    }
                }
            }
        }

        // Check if we're in Select mode
        if (ViewModel.SelectedTool == DrawingTool.Select)
        {
            // First check if we're clicking on a resize handle of selected shape
            if (_selectedShape != null)
            {
                _activeHandle = ViewModel.ManipulationService.GetHandleAtPoint(_selectedShape, _startPoint.X, _startPoint.Y, HandleSize);
                if (_activeHandle != ResizeHandle.None)
                {
                    _isResizing = true;
                    _lastDragPoint = _startPoint;
                    return;
                }
            }
            
            // Find shape under cursor
            var hitShape = ViewModel.ShapeService.HitTest(_startPoint.X, _startPoint.Y);
            if (hitShape != null)
            {
                // Deselect previous shape
                if (_selectedShape != null)
                    _selectedShape.IsSelected = false;
                
                // Select new shape
                _selectedShape = hitShape;
                _selectedShape.IsSelected = true;
                _isDragging = true;
                _lastDragPoint = _startPoint;
                ViewModel.SelectedShape = hitShape;
                RenderShapes();
                return;
            }
            else
            {
                // Clicked on empty space - deselect
                if (_selectedShape != null)
                {
                    _selectedShape.IsSelected = false;
                    _selectedShape = null;
                    ViewModel.SelectedShape = null;
                    RenderShapes();
                }
            }
        }
        else
        {
            // Drawing mode
            _isDrawing = true;
        }
    }

    private void DrawingCanvas_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (ViewModel == null || _drawingCanvas == null) return;

        var currentPoint = e.GetPosition(_drawingCanvas);

        // Dragging Polygon vertex live
        if (_isDragging && _selectedShape is Core.Models.Polygon polygon && _activePolygonVertexIndex >= 0)
        {
            // Move vertex directly (we need to bake transform first if present)
            if (polygon.Transform != Matrix3x3.Identity)
            {
                polygon.BakeTransform();
            }
            polygon.Vertices[_activePolygonVertexIndex] = new Point2D(currentPoint.X, currentPoint.Y);
            _lastDragPoint = currentPoint;
            RenderShapes();
            return;
        }

        // Dragging Bezier control point live
        if (_isDragging && _selectedShape is BezierCurve bez && _activeBezierControlPointIndex >= 0)
        {
            bez.ControlPoints[_activeBezierControlPointIndex] = new Point2D(currentPoint.X, currentPoint.Y);
            _lastDragPoint = currentPoint;
            RenderShapes();
            return;
        }

        // Handle resizing
        if (_isResizing && _selectedShape != null && ViewModel != null)
        {
            var deltaX = currentPoint.X - _lastDragPoint.X;
            var deltaY = currentPoint.Y - _lastDragPoint.Y;
            var mousePos = new Point2D(currentPoint.X, currentPoint.Y);
            ViewModel.ManipulationService.ResizeShape(_selectedShape, _activeHandle, deltaX, deltaY, mousePos);
            _lastDragPoint = currentPoint;
            RenderShapes();
            return;
        }

        // Handle dragging selected shape
        if (_isDragging && _selectedShape != null)
        {
            var deltaX = currentPoint.X - _lastDragPoint.X;
            var deltaY = currentPoint.Y - _lastDragPoint.Y;
            
            _selectedShape.Move(deltaX, deltaY);
            _lastDragPoint = currentPoint;
            RenderShapes();
            return;
        }
        
        // Preview while drawing (optional enhancement)
        if (_isDrawing)
        {
            // Could add preview rendering here
        }
    }

    private void DrawingCanvas_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (ViewModel == null || _drawingCanvas == null) return;

        // If finishing dragging a Polygon vertex
        if (_isDragging && _selectedShape is Core.Models.Polygon && _activePolygonVertexIndex >= 0)
        {
            _isDragging = false;
            _activePolygonVertexIndex = -1;
            ViewModel.UpdateParametersFromSelectedShape();
            return;
        }

        // If finishing dragging a Bezier control point
        if (_isDragging && _selectedShape is BezierCurve && _activeBezierControlPointIndex >= 0)
        {
            _isDragging = false;
            _activeBezierControlPointIndex = -1;
            ViewModel.UpdateParametersFromSelectedShape();
            return;
        }

        // Stop resizing
        if (_isResizing)
        {
            _isResizing = false;
            _activeHandle = ResizeHandle.None;
            // Update ViewModel parameters after resizing
            ViewModel.UpdateParametersFromSelectedShape();
            return;
        }

        // Stop dragging
        if (_isDragging)
        {
            _isDragging = false;
            // Update ViewModel parameters after dragging
            ViewModel.UpdateParametersFromSelectedShape();
            return;
        }
        
        if (!_isDrawing) return;
        
        _isDrawing = false;
        var endPoint = e.GetPosition(_drawingCanvas);

        // Create shape based on selected tool
        if (ViewModel.SelectedTool == DrawingTool.Line)
        {
            ViewModel.LineX1 = _startPoint.X.ToString();
            ViewModel.LineY1 = _startPoint.Y.ToString();
            ViewModel.LineX2 = endPoint.X.ToString();
            ViewModel.LineY2 = endPoint.Y.ToString();
            if (ViewModel.AddLineFromParamsCommand.CanExecute(null))
                ViewModel.AddLineFromParamsCommand.Execute(null);
        }
        else if (ViewModel.SelectedTool == DrawingTool.Rectangle)
        {
            var x = Math.Min(_startPoint.X, endPoint.X);
            var y = Math.Min(_startPoint.Y, endPoint.Y);
            var width = Math.Abs(endPoint.X - _startPoint.X);
            var height = Math.Abs(endPoint.Y - _startPoint.Y);
            
            ViewModel.RectX = x.ToString();
            ViewModel.RectY = y.ToString();
            ViewModel.RectWidth = width.ToString();
            ViewModel.RectHeight = height.ToString();
            if (ViewModel.AddRectangleFromParamsCommand.CanExecute(null))
                ViewModel.AddRectangleFromParamsCommand.Execute(null);
        }
        else if (ViewModel.SelectedTool == DrawingTool.Circle)
        {
            var radius = Math.Sqrt(
                Math.Pow(endPoint.X - _startPoint.X, 2) + 
                Math.Pow(endPoint.Y - _startPoint.Y, 2));
            
            ViewModel.CircleX = _startPoint.X.ToString();
            ViewModel.CircleY = _startPoint.Y.ToString();
            ViewModel.CircleRadius = radius.ToString();
            if (ViewModel.AddCircleFromParamsCommand.CanExecute(null))
                ViewModel.AddCircleFromParamsCommand.Execute(null);
        }
    }

    private void RenderShapes()
    {
        if (_drawingCanvas == null || ViewModel == null) return;
        
        _drawingCanvas.Children.Clear();

        foreach (var shape in ViewModel.ShapeService.Shapes)
        {
            if (shape is Core.Models.Line line)
            {
                var lineControl = new Avalonia.Controls.Shapes.Line
                {
                    StartPoint = new Avalonia.Point(line.StartPoint.X, line.StartPoint.Y),
                    EndPoint = new Avalonia.Point(line.EndPoint.X, line.EndPoint.Y),
                    Stroke = new SolidColorBrush(Color.FromUInt32(line.StrokeColor)),
                    StrokeThickness = line.IsSelected ? line.StrokeThickness + 2 : line.StrokeThickness
                };
                
                if (line.IsSelected)
                {
                    var highlightLine = new Avalonia.Controls.Shapes.Line
                    {
                        StartPoint = lineControl.StartPoint,
                        EndPoint = lineControl.EndPoint,
                        Stroke = Brushes.Blue,
                        StrokeThickness = line.StrokeThickness + 4,
                        Opacity = 0.3
                    };
                    _drawingCanvas.Children.Add(highlightLine);
                }
                
                _drawingCanvas.Children.Add(lineControl);
            }
            else if (shape is Core.Models.Rectangle rect)
            {
                if (rect.IsSelected)
                {
                    var highlight = new Avalonia.Controls.Shapes.Rectangle
                    {
                        Width = rect.Width + 8,
                        Height = rect.Height + 8,
                        Stroke = Brushes.Blue,
                        StrokeThickness = 2,
                        StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 4, 4 },
                        Fill = null
                    };
                    Canvas.SetLeft(highlight, rect.X - 4);
                    Canvas.SetTop(highlight, rect.Y - 4);
                    _drawingCanvas.Children.Add(highlight);
                }
                
                var rectControl = new Avalonia.Controls.Shapes.Rectangle
                {
                    Width = rect.Width,
                    Height = rect.Height,
                    Stroke = new SolidColorBrush(Color.FromUInt32(rect.StrokeColor)),
                    Fill = rect.FillColor.HasValue ? new SolidColorBrush(Color.FromUInt32(rect.FillColor.Value)) : null,
                    StrokeThickness = rect.StrokeThickness
                };
                Canvas.SetLeft(rectControl, rect.X);
                Canvas.SetTop(rectControl, rect.Y);
                _drawingCanvas.Children.Add(rectControl);
            }
            else if (shape is Core.Models.Circle circle)
            {
                if (circle.IsSelected)
                {
                    var highlight = new Ellipse
                    {
                        Width = (circle.Radius + 4) * 2,
                        Height = (circle.Radius + 4) * 2,
                        Stroke = Brushes.Blue,
                        StrokeThickness = 2,
                        StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 4, 4 },
                        Fill = null
                    };
                    Canvas.SetLeft(highlight, circle.Center.X - circle.Radius - 4);
                    Canvas.SetTop(highlight, circle.Center.Y - circle.Radius - 4);
                    _drawingCanvas.Children.Add(highlight);
                }
                
                var ellipse = new Ellipse
                {
                    Width = circle.Radius * 2,
                    Height = circle.Radius * 2,
                    Stroke = new SolidColorBrush(Color.FromUInt32(circle.StrokeColor)),
                    Fill = circle.FillColor.HasValue ? new SolidColorBrush(Color.FromUInt32(circle.FillColor.Value)) : null,
                    StrokeThickness = circle.StrokeThickness
                };
                Canvas.SetLeft(ellipse, circle.Center.X - circle.Radius);
                Canvas.SetTop(ellipse, circle.Center.Y - circle.Radius);
                _drawingCanvas.Children.Add(ellipse);
            }
            else if (shape is BezierCurve bez)
            {
                // Draw sampled curve as segments
                var pts = bez.SampleCurve(128);
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    var seg = new Avalonia.Controls.Shapes.Line
                    {
                        StartPoint = new Avalonia.Point(pts[i].X, pts[i].Y),
                        EndPoint = new Avalonia.Point(pts[i + 1].X, pts[i + 1].Y),
                        Stroke = new SolidColorBrush(Color.FromUInt32(bez.StrokeColor)),
                        StrokeThickness = bez.IsSelected ? bez.StrokeThickness + 1 : bez.StrokeThickness
                    };
                    _drawingCanvas.Children.Add(seg);
                }

                // Control polygon and points if selected
                if (bez.IsSelected)
                {
                    for (int i = 0; i < bez.ControlPoints.Count - 1; i++)
                    {
                        var cp1 = bez.ControlPoints[i];
                        var cp2 = bez.ControlPoints[i + 1];
                        var polySeg = new Avalonia.Controls.Shapes.Line
                        {
                            StartPoint = new Avalonia.Point(cp1.X, cp1.Y),
                            EndPoint = new Avalonia.Point(cp2.X, cp2.Y),
                            Stroke = Brushes.Gray,
                            StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 3, 3 },
                            StrokeThickness = 1
                        };
                        _drawingCanvas.Children.Add(polySeg);
                    }

                    const double r = 4;
                    for (int i = 0; i < bez.ControlPoints.Count; i++)
                    {
                        var cp = bez.ControlPoints[i];
                        var dot = new Ellipse
                        {
                            Width = r * 2,
                            Height = r * 2,
                            Fill = Brushes.White,
                            Stroke = Brushes.Blue,
                            StrokeThickness = 2
                        };
                        Canvas.SetLeft(dot, cp.X - r);
                        Canvas.SetTop(dot, cp.Y - r);
                        _drawingCanvas.Children.Add(dot);
                    }
                }
            }
            else if (shape is Core.Models.Polygon polygon)
            {
                var vertices = polygon.GetTransformedVertices();
                if (vertices.Count >= 2)
                {
                    // Create a path geometry for the polygon
                    var pathFigure = new PathFigure
                    {
                        StartPoint = new Avalonia.Point(vertices[0].X, vertices[0].Y),
                        IsClosed = polygon.IsClosed
                    };

                    for (int i = 1; i < vertices.Count; i++)
                    {
                        pathFigure.Segments!.Add(new LineSegment { Point = new Avalonia.Point(vertices[i].X, vertices[i].Y) });
                    }

                    var pathGeometry = new PathGeometry();
                    pathGeometry.Figures!.Add(pathFigure);

                    var path = new Path
                    {
                        Data = pathGeometry,
                        Stroke = new SolidColorBrush(Color.FromUInt32(polygon.StrokeColor)),
                        Fill = polygon.FillColor.HasValue ? new SolidColorBrush(Color.FromUInt32(polygon.FillColor.Value)) : null,
                        StrokeThickness = polygon.IsSelected ? polygon.StrokeThickness + 1 : polygon.StrokeThickness
                    };
                    _drawingCanvas.Children.Add(path);

                    // Draw selection highlight
                    if (polygon.IsSelected)
                    {
                        var highlightPath = new Path
                        {
                            Data = pathGeometry,
                            Stroke = Brushes.Blue,
                            StrokeThickness = polygon.StrokeThickness + 3,
                            Opacity = 0.3,
                            Fill = null
                        };
                        _drawingCanvas.Children.Insert(_drawingCanvas.Children.Count - 1, highlightPath);

                        // Draw vertex handles
                        for (int i = 0; i < vertices.Count; i++)
                        {
                            var v = vertices[i];
                            var dot = new Ellipse
                            {
                                Width = VertexHandleRadius * 2,
                                Height = VertexHandleRadius * 2,
                                Fill = Brushes.White,
                                Stroke = Brushes.Blue,
                                StrokeThickness = 2
                            };
                            Canvas.SetLeft(dot, v.X - VertexHandleRadius);
                            Canvas.SetTop(dot, v.Y - VertexHandleRadius);
                            _drawingCanvas.Children.Add(dot);
                        }
                    }
                }
            }
        }
        
        // Resize handles for non-Bezier and non-Polygon shapes
        if (_selectedShape != null && _selectedShape is not BezierCurve && _selectedShape is not Core.Models.Polygon)
        {
            DrawResizeHandles(_selectedShape);
        }
    }
    
    private void DrawResizeHandles(IShape shape)
    {
        if (_drawingCanvas == null || ViewModel == null) return;
        
        var handlePositions = ViewModel.ManipulationService.GetHandlePositions(shape);
        
        foreach (var (x, y, handleType) in handlePositions)
        {
            // Corner handles are white with blue border, mid-point handles are light blue
            var isCornerHandle = handleType == ResizeHandle.TopLeft || 
                                handleType == ResizeHandle.TopRight || 
                                handleType == ResizeHandle.BottomLeft || 
                                handleType == ResizeHandle.BottomRight;
            
            var handle = new Border
            {
                Width = HandleSize,
                Height = HandleSize,
                Background = isCornerHandle ? Brushes.White : Brushes.LightBlue,
                BorderBrush = Brushes.Blue,
                BorderThickness = new Avalonia.Thickness(isCornerHandle ? 2 : 1)
            };
            Canvas.SetLeft(handle, x - HandleSize / 2);
            Canvas.SetTop(handle, y - HandleSize / 2);
            _drawingCanvas.Children.Add(handle);
        }
    }
}
