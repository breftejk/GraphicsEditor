using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using GraphicsEditor.UI.ViewModels;
using System;
using System.Collections.Generic;

namespace GraphicsEditor.UI.Views;

public partial class ThreeDView : UserControl
{
    private ThreeDViewModel? ViewModel => DataContext as ThreeDViewModel;
    private Canvas? _rgbCubeCanvas;
    private bool _isDragging = false;
    private Avalonia.Point _lastMousePosition;

    public ThreeDView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (ViewModel != null)
        {
            _rgbCubeCanvas = this.FindControl<Canvas>("RgbCubeCanvas");
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.RotationX) ||
                    e.PropertyName == nameof(ViewModel.RotationY) ||
                    e.PropertyName == nameof(ViewModel.RotationZ))
                {
                    RenderCube();
                }
            };
            RenderCube();
        }
    }

    private void Display3DCanvas_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isDragging = true;
        _lastMousePosition = e.GetPosition(sender as Canvas);
    }

    private void Display3DCanvas_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging || ViewModel == null) return;

        var currentPosition = e.GetPosition(sender as Canvas);
        var delta = currentPosition - _lastMousePosition;

        // Rotate based on mouse movement
        ViewModel.RotationY += delta.X * 0.5;
        ViewModel.RotationX += delta.Y * 0.5;

        _lastMousePosition = currentPosition;
    }

    private void Display3DCanvas_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
    }

    private void RenderCube()
    {
        if (_rgbCubeCanvas == null || ViewModel == null) return;

        // Clear existing content except static text
        var itemsToRemove = new List<Control>();
        foreach (var child in _rgbCubeCanvas.Children)
        {
            if (child is not TextBlock)
                itemsToRemove.Add(child);
        }
        foreach (var item in itemsToRemove)
        {
            _rgbCubeCanvas.Children.Remove(item);
        }

        // Get canvas dimensions
        double canvasWidth = _rgbCubeCanvas.Bounds.Width > 0 ? _rgbCubeCanvas.Bounds.Width : 800;
        double canvasHeight = _rgbCubeCanvas.Bounds.Height > 0 ? _rgbCubeCanvas.Bounds.Height : 600;
        double centerX = canvasWidth / 2;
        double centerY = canvasHeight / 2;
        double scale = 150; // Scale factor for the cube

        // Generate cube vertices
        var vertices = ViewModel.ThreeDService.GenerateRgbCubeVertices(5);
        
        // Rotate vertices
        var rotatedVertices = new List<(double X, double Y, double Z, Core.Models.RgbColor Color)>();
        foreach (var vertex in vertices)
        {
            var rotated = RotateVertex(vertex.X, vertex.Y, vertex.Z, 
                ViewModel.RotationX, ViewModel.RotationY, ViewModel.RotationZ);
            rotatedVertices.Add((rotated.X, rotated.Y, rotated.Z, vertex.Color));
        }

        // Sort by Z for proper depth rendering (painter's algorithm)
        rotatedVertices.Sort((a, b) => a.Z.CompareTo(b.Z));

        // Draw vertices as points
        foreach (var vertex in rotatedVertices)
        {
            // Project 3D to 2D (simple orthographic projection)
            double x2d = centerX + vertex.X * scale;
            double y2d = centerY - vertex.Y * scale; // Invert Y for screen coordinates

            var point = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(Color.FromRgb(vertex.Color.R, vertex.Color.G, vertex.Color.B)),
                Stroke = Brushes.White,
                StrokeThickness = 1
            };

            Canvas.SetLeft(point, x2d - 4);
            Canvas.SetTop(point, y2d - 4);
            _rgbCubeCanvas.Children.Add(point);
        }

        // Draw cube edges (8 vertices, 12 edges)
        DrawCubeEdges(centerX, centerY, scale);
    }

    private void DrawCubeEdges(double centerX, double centerY, double scale)
    {
        if (_rgbCubeCanvas == null || ViewModel == null) return;

        // Define the 8 corners of a unit cube in 0-1 space (same as vertices)
        var corners = new[]
        {
            (0.0, 0.0, 0.0), // 0: Black
            (1.0, 0.0, 0.0), // 1: Red
            (0.0, 1.0, 0.0), // 2: Green
            (1.0, 1.0, 0.0), // 3: Yellow
            (0.0, 0.0, 1.0), // 4: Blue
            (1.0, 0.0, 1.0), // 5: Magenta
            (0.0, 1.0, 1.0), // 6: Cyan
            (1.0, 1.0, 1.0), // 7: White
        };

        // Rotate all corners
        var rotatedCorners = new List<(double X, double Y, double Z)>();
        foreach (var corner in corners)
        {
            rotatedCorners.Add(RotateVertex(corner.Item1, corner.Item2, corner.Item3,
                ViewModel.RotationX, ViewModel.RotationY, ViewModel.RotationZ));
        }

        // Define the 12 edges
        var edges = new[]
        {
            (0, 1), (2, 3), (4, 5), (6, 7), // 4 parallel to X
            (0, 2), (1, 3), (4, 6), (5, 7), // 4 parallel to Y
            (0, 4), (1, 5), (2, 6), (3, 7), // 4 parallel to Z
        };

        // Draw each edge
        foreach (var (start, end) in edges)
        {
            var p1 = rotatedCorners[start];
            var p2 = rotatedCorners[end];

            var line = new Line
            {
                StartPoint = new Avalonia.Point(centerX + p1.X * scale, centerY - p1.Y * scale),
                EndPoint = new Avalonia.Point(centerX + p2.X * scale, centerY - p2.Y * scale),
                Stroke = Brushes.Gray,
                StrokeThickness = 1
            };

            _rgbCubeCanvas.Children.Add(line);
        }
    }

    private (double X, double Y, double Z) RotateVertex(double x, double y, double z, 
        double rotX, double rotY, double rotZ)
    {
        // Translate to origin (center cube at 0,0,0) - cube is in 0-1 space, so center is at 0.5
        double xc = x - 0.5;
        double yc = y - 0.5;
        double zc = z - 0.5;
        
        // Convert degrees to radians
        double radX = rotX * Math.PI / 180.0;
        double radY = rotY * Math.PI / 180.0;
        double radZ = rotZ * Math.PI / 180.0;

        // Rotate around X axis
        double y1 = yc * Math.Cos(radX) - zc * Math.Sin(radX);
        double z1 = yc * Math.Sin(radX) + zc * Math.Cos(radX);

        // Rotate around Y axis
        double x2 = xc * Math.Cos(radY) + z1 * Math.Sin(radY);
        double z2 = -xc * Math.Sin(radY) + z1 * Math.Cos(radY);

        // Rotate around Z axis
        double x3 = x2 * Math.Cos(radZ) - y1 * Math.Sin(radZ);
        double y3 = x2 * Math.Sin(radZ) + y1 * Math.Cos(radZ);

        // No need to translate back - return centered coordinates
        return (x3, y3, z2);
    }
}
