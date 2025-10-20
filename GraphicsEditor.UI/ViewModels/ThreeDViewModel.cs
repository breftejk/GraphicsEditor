using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraphicsEditor.Logic;

namespace GraphicsEditor.UI.ViewModels;

/// <summary>
/// View model for 3D visualization (RGB Cube).
/// </summary>
public partial class ThreeDViewModel : ViewModelBase
{
    private readonly ThreeDRenderingService _renderingService;

    // Public accessor for the rendering service
    public ThreeDRenderingService ThreeDService => _renderingService;

    // Available axes for cross-section
    public IEnumerable<CrossSectionAxis> AvailableAxes { get; } = Enum.GetValues<CrossSectionAxis>();

    [ObservableProperty]
    private double _rotationX;

    [ObservableProperty]
    private double _rotationY;

    [ObservableProperty]
    private double _rotationZ;

    [ObservableProperty]
    private CrossSectionAxis _selectedAxis = CrossSectionAxis.X;

    [ObservableProperty]
    private double _crossSectionPosition = 0.5;

    [ObservableProperty]
    private bool _showCrossSection;

    [ObservableProperty]
    private List<RgbCubeVertex> _cubeVertices;

    [ObservableProperty]
    private List<RgbCubeVertex> _crossSectionVertices;

    [ObservableProperty]
    private string _statusMessage = "RGB Cube Visualization";

    public ThreeDViewModel()
    {
        _renderingService = new ThreeDRenderingService();
        _cubeVertices = new List<RgbCubeVertex>();
        _crossSectionVertices = new List<RgbCubeVertex>();
        GenerateCube();
    }

    [RelayCommand]
    private void GenerateCube()
    {
        CubeVertices = _renderingService.GenerateRgbCubeVertices(10);
        UpdateCrossSection();
        StatusMessage = $"Generated RGB Cube with {CubeVertices.Count} vertices";
    }

    [RelayCommand]
    private void RotateLeft()
    {
        RotationY -= 15;
        StatusMessage = $"Rotation: X={RotationX:F0}° Y={RotationY:F0}° Z={RotationZ:F0}°";
    }

    [RelayCommand]
    private void RotateRight()
    {
        RotationY += 15;
        StatusMessage = $"Rotation: X={RotationX:F0}° Y={RotationY:F0}° Z={RotationZ:F0}°";
    }

    [RelayCommand]
    private void RotateUp()
    {
        RotationX -= 15;
        StatusMessage = $"Rotation: X={RotationX:F0}° Y={RotationY:F0}° Z={RotationZ:F0}°";
    }

    [RelayCommand]
    private void RotateDown()
    {
        RotationX += 15;
        StatusMessage = $"Rotation: X={RotationX:F0}° Y={RotationY:F0}° Z={RotationZ:F0}°";
    }

    [RelayCommand]
    private void ResetRotation()
    {
        RotationX = 0;
        RotationY = 0;
        RotationZ = 0;
        StatusMessage = "Rotation reset";
    }

    [RelayCommand]
    private void UpdateCrossSection()
    {
        if (!ShowCrossSection) return;

        CrossSectionVertices = _renderingService.GenerateRgbCubeCrossSection(
            SelectedAxis, 
            CrossSectionPosition, 
            50);

        string axisName = SelectedAxis switch
        {
            CrossSectionAxis.X => "Red",
            CrossSectionAxis.Y => "Green",
            CrossSectionAxis.Z => "Blue",
            _ => "Unknown"
        };

        StatusMessage = $"Cross-section: {axisName} axis at {CrossSectionPosition:F2}";
    }

    partial void OnSelectedAxisChanged(CrossSectionAxis value) => UpdateCrossSection();
    partial void OnCrossSectionPositionChanged(double value) => UpdateCrossSection();
    partial void OnShowCrossSectionChanged(bool value)
    {
        if (value)
            UpdateCrossSection();
    }

    /// <summary>
    /// Handles mouse drag to rotate the cube.
    /// </summary>
    public void OnMouseDrag(double deltaX, double deltaY)
    {
        RotationY += deltaX * 0.5;
        RotationX += deltaY * 0.5;
    }
}
