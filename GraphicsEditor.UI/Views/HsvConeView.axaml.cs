using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using GraphicsEditor.UI.ViewModels;
using System;
using System.Collections.Generic;

namespace GraphicsEditor.UI.Views;

public partial class HsvConeView : UserControl
{
    private HsvConeViewModel? ViewModel => DataContext as HsvConeViewModel;
    private Canvas? _hsvConeCanvas;
    private bool _isDragging = false;
    private Avalonia.Point _lastMousePosition;

    public HsvConeView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (ViewModel != null)
        {
            _hsvConeCanvas = this.FindControl<Canvas>("HsvConeCanvas");
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.RotationX) ||
                    e.PropertyName == nameof(ViewModel.RotationY) ||
                    e.PropertyName == nameof(ViewModel.RotationZ) ||
                    e.PropertyName == nameof(ViewModel.ShowCrossSection) ||
                    e.PropertyName == nameof(ViewModel.CrossSectionVertices))
                {
                    RenderCone();
                }
            };
            RenderCone();
        }
    }

    private void DisplayConeCanvas_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isDragging = true;
        _lastMousePosition = e.GetPosition(sender as Canvas);
    }

    private void DisplayConeCanvas_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging || ViewModel == null) return;

        var currentPosition = e.GetPosition(sender as Canvas);
        var delta = currentPosition - _lastMousePosition;

        ViewModel.RotationY += delta.X * 0.5;
        ViewModel.RotationX += delta.Y * 0.5;

        _lastMousePosition = currentPosition;
    }

    private void DisplayConeCanvas_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
    }

    private void RenderCone()
    {
        if (_hsvConeCanvas == null || ViewModel == null) return;

        // Clear existing content except static text
        var itemsToRemove = new List<Control>();
        foreach (var child in _hsvConeCanvas.Children)
        {
            if (child is not TextBlock)
                itemsToRemove.Add(child);
        }
        foreach (var item in itemsToRemove)
        {
            _hsvConeCanvas.Children.Remove(item);
        }

        double canvasWidth = _hsvConeCanvas.Bounds.Width > 0 ? _hsvConeCanvas.Bounds.Width : 800;
        double canvasHeight = _hsvConeCanvas.Bounds.Height > 0 ? _hsvConeCanvas.Bounds.Height : 600;
        double centerX = canvasWidth / 2;
        double centerY = canvasHeight / 2;
        double scale = 200;

        var vertices = ViewModel.ConeVertices;
        var rotatedVertices = new List<(double X, double Y, double Z, byte R, byte G, byte B)>(vertices.Count);

        foreach (var vertex in vertices)
        {
            var rotated = RotateVertex(vertex.X, vertex.Y, vertex.Z,
                ViewModel.RotationX, ViewModel.RotationY, ViewModel.RotationZ);
            rotatedVertices.Add((rotated.X, rotated.Y, rotated.Z, 
                vertex.RgbColor.R, vertex.RgbColor.G, vertex.RgbColor.B));
        }

        rotatedVertices.Sort((a, b) => a.Z.CompareTo(b.Z));

        foreach (var vertex in rotatedVertices)
        {
            double x2d = centerX + vertex.X * scale;
            double y2d = centerY - vertex.Y * scale;

            var point = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(Color.FromRgb(vertex.R, vertex.G, vertex.B)),
                Stroke = Brushes.Gray,
                StrokeThickness = 0.5
            };

            Canvas.SetLeft(point, x2d - 3);
            Canvas.SetTop(point, y2d - 3);
            _hsvConeCanvas.Children.Add(point);
        }

        // Draw cross-section if enabled
        if (ViewModel.ShowCrossSection && ViewModel.CrossSectionVertices.Count > 0)
        {
            DrawCrossSection(centerX, centerY, scale);
        }
    }

    private void DrawCrossSection(double centerX, double centerY, double scale)
    {
        if (_hsvConeCanvas == null || ViewModel == null) return;

        var crossSectionVertices = ViewModel.CrossSectionVertices;
        if (crossSectionVertices == null || crossSectionVertices.Count == 0) return;

        var rotatedVertices = new List<(double X, double Y, double Z, byte R, byte G, byte B)>(crossSectionVertices.Count);

        foreach (var vertex in crossSectionVertices)
        {
            var rotated = RotateVertex(vertex.X, vertex.Y, vertex.Z,
                ViewModel.RotationX, ViewModel.RotationY, ViewModel.RotationZ);
            rotatedVertices.Add((rotated.X, rotated.Y, rotated.Z,
                vertex.RgbColor.R, vertex.RgbColor.G, vertex.RgbColor.B));
        }

        rotatedVertices.Sort((a, b) => a.Z.CompareTo(b.Z));

        // Draw filled cross-section based on type
        if (ViewModel.SelectedCrossSectionType == GraphicsEditor.Logic.HsvCrossSectionType.Horizontal)
        {
            // Horizontal cross-section is circular - draw as filled circle with triangular segments
            DrawFilledCircleCrossSection(rotatedVertices, centerX, centerY, scale);
        }
        else
        {
            // Vertical cross-section is triangular - draw as filled triangle
            DrawFilledTriangleCrossSection(rotatedVertices, centerX, centerY, scale);
        }
    }

    private void DrawFilledCircleCrossSection(List<(double X, double Y, double Z, byte R, byte G, byte B)> vertices, 
        double centerX, double centerY, double scale)
    {
        if (vertices.Count < 3) return;

        // Draw circle as filled polygon
        var points = new List<Avalonia.Point>();
        
        foreach (var v in vertices)
        {
            points.Add(new Avalonia.Point(centerX + v.X * scale, centerY - v.Y * scale));
        }
        
        var polygon = new Polygon
        {
            Fill = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
            Stroke = null,
            Points = points
        };
        
        _hsvConeCanvas!.Children.Add(polygon);
    }

    private void DrawFilledTriangleCrossSection(List<(double X, double Y, double Z, byte R, byte G, byte B)> vertices,
        double centerX, double centerY, double scale)
    {
        if (vertices.Count < 3) return;

        // Just draw the 3 points as a triangle
        var polygon = new Polygon
        {
            Fill = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
            Stroke = null,
            Points = new List<Avalonia.Point>
            {
                new Avalonia.Point(centerX + vertices[0].X * scale, centerY - vertices[0].Y * scale),
                new Avalonia.Point(centerX + vertices[1].X * scale, centerY - vertices[1].Y * scale),
                new Avalonia.Point(centerX + vertices[2].X * scale, centerY - vertices[2].Y * scale)
            }
        };

        _hsvConeCanvas!.Children.Add(polygon);
    }

    private (double X, double Y, double Z) RotateVertex(double x, double y, double z,
        double rotX, double rotY, double rotZ)
    {
        // Convert degrees to radians
        double radX = rotX * Math.PI / 180.0;
        double radY = rotY * Math.PI / 180.0;
        double radZ = rotZ * Math.PI / 180.0;

        // Rotate around X axis
        double y1 = y * Math.Cos(radX) - z * Math.Sin(radX);
        double z1 = y * Math.Sin(radX) + z * Math.Cos(radX);

        // Rotate around Y axis
        double x2 = x * Math.Cos(radY) + z1 * Math.Sin(radY);
        double z2 = -x * Math.Sin(radY) + z1 * Math.Cos(radY);

        // Rotate around Z axis
        double x3 = x2 * Math.Cos(radZ) - y1 * Math.Sin(radZ);
        double y3 = x2 * Math.Sin(radZ) + y1 * Math.Cos(radZ);

        return (x3, y3, z2);
    }
}
