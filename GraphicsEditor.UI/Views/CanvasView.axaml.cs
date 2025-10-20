using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using GraphicsEditor.UI.ViewModels;
using GraphicsEditor.Core.Models;
using GraphicsEditor.Core.Geometry;
using System;

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
            };
            _drawingCanvas = this.FindControl<Canvas>("DrawingCanvas");
            RenderShapes();
        }
    }

    private void DrawingCanvas_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel == null || _drawingCanvas == null) return;
        
        _startPoint = e.GetPosition(_drawingCanvas);

        // Check if we're in Select mode
        if (ViewModel.SelectedTool == DrawingTool.Select)
        {
            // First check if we're clicking on a resize handle of selected shape
            if (_selectedShape != null)
            {
                _activeHandle = GetHandleAtPoint(_selectedShape, _startPoint.X, _startPoint.Y);
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

        // Handle resizing
        if (_isResizing && _selectedShape != null)
        {
            ResizeShape(_selectedShape, _activeHandle, currentPoint.X - _lastDragPoint.X, currentPoint.Y - _lastDragPoint.Y);
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

        // Stop resizing
        if (_isResizing)
        {
            _isResizing = false;
            _activeHandle = ResizeHandle.None;
            return;
        }

        // Stop dragging
        if (_isDragging)
        {
            _isDragging = false;
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
                
                // Add selection highlight
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
                // Add selection highlight
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
                // Add selection highlight
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
        }
        
        // Draw resize handles for selected shape
        if (_selectedShape != null)
        {
            DrawResizeHandles(_selectedShape);
        }
    }
    
    private void DrawResizeHandles(IShape shape)
    {
        if (_drawingCanvas == null) return;
        
        var bounds = shape.GetBounds();
        var handles = new[]
        {
            (bounds.X, bounds.Y), // Top-left
            (bounds.X + bounds.Width, bounds.Y), // Top-right
            (bounds.X, bounds.Y + bounds.Height), // Bottom-left
            (bounds.X + bounds.Width, bounds.Y + bounds.Height), // Bottom-right
        };
        
        foreach (var (x, y) in handles)
        {
            var handle = new Border
            {
                Width = HandleSize,
                Height = HandleSize,
                Background = Brushes.White,
                BorderBrush = Brushes.Blue,
                BorderThickness = new Avalonia.Thickness(2)
            };
            Canvas.SetLeft(handle, x - HandleSize / 2);
            Canvas.SetTop(handle, y - HandleSize / 2);
            _drawingCanvas.Children.Add(handle);
        }
        
        // Mid-point handles for rectangles and lines
        if (shape is Core.Models.Rectangle || shape is Core.Models.Line)
        {
            var midHandles = new[]
            {
                (bounds.X + bounds.Width / 2, bounds.Y), // Top
                (bounds.X + bounds.Width / 2, bounds.Y + bounds.Height), // Bottom
                (bounds.X, bounds.Y + bounds.Height / 2), // Left
                (bounds.X + bounds.Width, bounds.Y + bounds.Height / 2), // Right
            };
            
            foreach (var (x, y) in midHandles)
            {
                var handle = new Border
                {
                    Width = HandleSize,
                    Height = HandleSize,
                    Background = Brushes.LightBlue,
                    BorderBrush = Brushes.Blue,
                    BorderThickness = new Avalonia.Thickness(1)
                };
                Canvas.SetLeft(handle, x - HandleSize / 2);
                Canvas.SetTop(handle, y - HandleSize / 2);
                _drawingCanvas.Children.Add(handle);
            }
        }
    }
    
    private ResizeHandle GetHandleAtPoint(IShape shape, double x, double y)
    {
        var bounds = shape.GetBounds();
        double tolerance = HandleSize / 2 + 2;
        
        // Check corner handles first
        if (Math.Abs(x - bounds.X) < tolerance && Math.Abs(y - bounds.Y) < tolerance)
            return ResizeHandle.TopLeft;
        if (Math.Abs(x - (bounds.X + bounds.Width)) < tolerance && Math.Abs(y - bounds.Y) < tolerance)
            return ResizeHandle.TopRight;
        if (Math.Abs(x - bounds.X) < tolerance && Math.Abs(y - (bounds.Y + bounds.Height)) < tolerance)
            return ResizeHandle.BottomLeft;
        if (Math.Abs(x - (bounds.X + bounds.Width)) < tolerance && Math.Abs(y - (bounds.Y + bounds.Height)) < tolerance)
            return ResizeHandle.BottomRight;
        
        // Check mid-point handles for rectangles and lines
        if (shape is Core.Models.Rectangle || shape is Core.Models.Line)
        {
            if (Math.Abs(x - (bounds.X + bounds.Width / 2)) < tolerance && Math.Abs(y - bounds.Y) < tolerance)
                return ResizeHandle.Top;
            if (Math.Abs(x - (bounds.X + bounds.Width / 2)) < tolerance && Math.Abs(y - (bounds.Y + bounds.Height)) < tolerance)
                return ResizeHandle.Bottom;
            if (Math.Abs(x - bounds.X) < tolerance && Math.Abs(y - (bounds.Y + bounds.Height / 2)) < tolerance)
                return ResizeHandle.Left;
            if (Math.Abs(x - (bounds.X + bounds.Width)) < tolerance && Math.Abs(y - (bounds.Y + bounds.Height / 2)) < tolerance)
                return ResizeHandle.Right;
        }
        
        return ResizeHandle.None;
    }
    
    private void ResizeShape(IShape shape, ResizeHandle handle, double deltaX, double deltaY)
    {
        if (shape is Core.Models.Rectangle rect)
        {
            switch (handle)
            {
                case ResizeHandle.TopLeft:
                    rect.X += deltaX;
                    rect.Y += deltaY;
                    rect.Width -= deltaX;
                    rect.Height -= deltaY;
                    break;
                case ResizeHandle.TopRight:
                    rect.Y += deltaY;
                    rect.Width += deltaX;
                    rect.Height -= deltaY;
                    break;
                case ResizeHandle.BottomLeft:
                    rect.X += deltaX;
                    rect.Width -= deltaX;
                    rect.Height += deltaY;
                    break;
                case ResizeHandle.BottomRight:
                    rect.Width += deltaX;
                    rect.Height += deltaY;
                    break;
                case ResizeHandle.Top:
                    rect.Y += deltaY;
                    rect.Height -= deltaY;
                    break;
                case ResizeHandle.Bottom:
                    rect.Height += deltaY;
                    break;
                case ResizeHandle.Left:
                    rect.X += deltaX;
                    rect.Width -= deltaX;
                    break;
                case ResizeHandle.Right:
                    rect.Width += deltaX;
                    break;
            }
            
            // Ensure minimum size
            if (rect.Width < 10) rect.Width = 10;
            if (rect.Height < 10) rect.Height = 10;
        }
        else if (shape is Core.Models.Circle circle)
        {
            // For circles, only corner handles work - they change the radius
            var centerX = circle.Center.X;
            var centerY = circle.Center.Y;
            
            switch (handle)
            {
                case ResizeHandle.TopLeft:
                case ResizeHandle.TopRight:
                case ResizeHandle.BottomLeft:
                case ResizeHandle.BottomRight:
                    // Calculate new radius based on distance from center to mouse position
                    var newRadius = Math.Sqrt(
                        Math.Pow(_lastDragPoint.X - centerX, 2) + 
                        Math.Pow(_lastDragPoint.Y - centerY, 2)
                    );
                    circle.Radius = Math.Max(5, newRadius); // Minimum radius of 5
                    break;
            }
        }
        else if (shape is Core.Models.Line line)
        {
            switch (handle)
            {
                case ResizeHandle.TopLeft:
                    line.StartPoint = new Point2D(line.StartPoint.X + deltaX, line.StartPoint.Y + deltaY);
                    break;
                case ResizeHandle.BottomRight:
                    line.EndPoint = new Point2D(line.EndPoint.X + deltaX, line.EndPoint.Y + deltaY);
                    break;
                case ResizeHandle.TopRight:
                case ResizeHandle.BottomLeft:
                    // For lines, top-right and bottom-left also move endpoints
                    var bounds = line.GetBounds();
                    if (line.StartPoint.Y < line.EndPoint.Y)
                    {
                        if (handle == ResizeHandle.TopRight)
                            line.StartPoint = new Point2D(line.StartPoint.X + deltaX, line.StartPoint.Y + deltaY);
                        else
                            line.EndPoint = new Point2D(line.EndPoint.X + deltaX, line.EndPoint.Y + deltaY);
                    }
                    else
                    {
                        if (handle == ResizeHandle.TopRight)
                            line.EndPoint = new Point2D(line.EndPoint.X + deltaX, line.EndPoint.Y + deltaY);
                        else
                            line.StartPoint = new Point2D(line.StartPoint.X + deltaX, line.StartPoint.Y + deltaY);
                    }
                    break;
                case ResizeHandle.Top:
                case ResizeHandle.Bottom:
                case ResizeHandle.Left:
                case ResizeHandle.Right:
                    // Mid-point handles move the closer endpoint
                    var midX = (line.StartPoint.X + line.EndPoint.X) / 2;
                    var midY = (line.StartPoint.Y + line.EndPoint.Y) / 2;
                    
                    if (handle == ResizeHandle.Top || handle == ResizeHandle.Bottom)
                    {
                        if (Math.Abs(line.StartPoint.Y - midY) < Math.Abs(line.EndPoint.Y - midY))
                            line.StartPoint = new Point2D(line.StartPoint.X, line.StartPoint.Y + deltaY);
                        else
                            line.EndPoint = new Point2D(line.EndPoint.X, line.EndPoint.Y + deltaY);
                    }
                    else // Left or Right
                    {
                        if (Math.Abs(line.StartPoint.X - midX) < Math.Abs(line.EndPoint.X - midX))
                            line.StartPoint = new Point2D(line.StartPoint.X + deltaX, line.StartPoint.Y);
                        else
                            line.EndPoint = new Point2D(line.EndPoint.X + deltaX, line.EndPoint.Y);
                    }
                    break;
            }
        }
    }
}

enum ResizeHandle
{
    None,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Top,
    Bottom,
    Left,
    Right
}
