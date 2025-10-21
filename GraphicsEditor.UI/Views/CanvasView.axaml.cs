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
