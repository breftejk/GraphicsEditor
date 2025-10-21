using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraphicsEditor.Logic;

namespace GraphicsEditor.UI.ViewModels;

/// <summary>
/// View model for HSV Cone visualization.
/// </summary>
public partial class HsvConeViewModel : ViewModelBase
{
    private readonly ThreeDRenderingService _renderingService;

    public ThreeDRenderingService ThreeDService => _renderingService;

    public IEnumerable<HsvCrossSectionType> AvailableCrossSectionTypes { get; } = Enum.GetValues<HsvCrossSectionType>();

    [ObservableProperty]
    private double _rotationX;

    [ObservableProperty]
    private double _rotationY;

    [ObservableProperty]
    private double _rotationZ;

    [ObservableProperty]
    private HsvCrossSectionType _selectedCrossSectionType = HsvCrossSectionType.Horizontal;

    [ObservableProperty]
    private double _crossSectionPosition = 0.5;

    [ObservableProperty]
    private bool _showCrossSection;

    [ObservableProperty]
    private List<HsvConeVertex> _coneVertices;

    [ObservableProperty]
    private List<HsvConeVertex> _crossSectionVertices;

    [ObservableProperty]
    private string _statusMessage = "HSV Cone Visualization";

    public HsvConeViewModel()
    {
        _renderingService = new ThreeDRenderingService();
        _coneVertices = new List<HsvConeVertex>();
        _crossSectionVertices = new List<HsvConeVertex>();
        GenerateCone();
    }

    [RelayCommand]
    private void GenerateCone()
    {
        ConeVertices = _renderingService.GenerateHsvConeVertices(36, 10);
        UpdateCrossSection();
        StatusMessage = $"Generated HSV Cone with {ConeVertices.Count} vertices";
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
        if (!ShowCrossSection)
        {
            CrossSectionVertices = new List<HsvConeVertex>();
            return;
        }

        CrossSectionVertices = _renderingService.GenerateHsvConeCrossSection(
            SelectedCrossSectionType,
            CrossSectionPosition,
            3);

        string typeName = SelectedCrossSectionType switch
        {
            HsvCrossSectionType.Horizontal => "Horizontal (Value)",
            HsvCrossSectionType.Vertical => "Vertical (Hue)",
            _ => "Unknown"
        };

        StatusMessage = $"Cross-section: {typeName} at {CrossSectionPosition:F2} ({CrossSectionVertices.Count} points)";
    }

    partial void OnSelectedCrossSectionTypeChanged(HsvCrossSectionType value) => UpdateCrossSection();
    partial void OnCrossSectionPositionChanged(double value) => UpdateCrossSection();
    partial void OnShowCrossSectionChanged(bool value)
    {
        if (value)
            UpdateCrossSection();
        else
            CrossSectionVertices = new List<HsvConeVertex>();
    }

    public void OnMouseDrag(double deltaX, double deltaY)
    {
        RotationY += deltaX * 0.5;
        RotationX += deltaY * 0.5;
    }
}
