using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraphicsEditor.Core.Models;
using GraphicsEditor.Logic;

namespace GraphicsEditor.UI.ViewModels;

/// <summary>
/// View model for the color converter tab.
/// Supports real-time RGB ↔ CMYK conversion with slider and text input.
/// </summary>
public partial class ColorConverterViewModel : ViewModelBase
{
    private readonly ColorConversionService _colorService;

    // RGB Values (0-255)
    [ObservableProperty]
    private byte _red;

    [ObservableProperty]
    private byte _green;

    [ObservableProperty]
    private byte _blue;

    // CMYK Values (0-100)
    [ObservableProperty]
    private double _cyan;

    [ObservableProperty]
    private double _magenta;

    [ObservableProperty]
    private double _yellow;

    [ObservableProperty]
    private double _key;

    [ObservableProperty]
    private string _hexColor = "#000000";

    [ObservableProperty]
    private uint _previewColor = 0xFF000000;

    [ObservableProperty]
    private bool _isUpdatingFromRgb;

    [ObservableProperty]
    private bool _isUpdatingFromCmyk;

    public ColorConverterViewModel()
    {
        _colorService = new ColorConversionService();
        _red = 0;
        _green = 0;
        _blue = 0;
        UpdateCmykFromRgb();
    }

    partial void OnRedChanged(byte value) => UpdateFromRgb();
    partial void OnGreenChanged(byte value) => UpdateFromRgb();
    partial void OnBlueChanged(byte value) => UpdateFromRgb();

    partial void OnCyanChanged(double value) => UpdateFromCmyk();
    partial void OnMagentaChanged(double value) => UpdateFromCmyk();
    partial void OnYellowChanged(double value) => UpdateFromCmyk();
    partial void OnKeyChanged(double value) => UpdateFromCmyk();

    private void UpdateFromRgb()
    {
        if (_isUpdatingFromCmyk) return;

        _isUpdatingFromRgb = true;

        var rgb = new RgbColor(Red, Green, Blue);
        UpdateCmykFromRgb();
        UpdatePreview();
        UpdateHex();

        _isUpdatingFromRgb = false;
    }

    private void UpdateFromCmyk()
    {
        if (_isUpdatingFromRgb) return;

        _isUpdatingFromCmyk = true;

        var cmyk = new CmykColor(Cyan, Magenta, Yellow, Key);
        var rgb = _colorService.ConvertCmykToRgb(cmyk);

        Red = rgb.R;
        Green = rgb.G;
        Blue = rgb.B;

        UpdatePreview();
        UpdateHex();

        _isUpdatingFromCmyk = false;
    }

    private void UpdateCmykFromRgb()
    {
        var rgb = new RgbColor(Red, Green, Blue);
        var cmyk = _colorService.ConvertRgbToCmyk(rgb);

        Cyan = cmyk.C;
        Magenta = cmyk.M;
        Yellow = cmyk.Y;
        Key = cmyk.K;
    }

    private void UpdatePreview()
    {
        PreviewColor = 0xFF000000 | ((uint)Red << 16) | ((uint)Green << 8) | Blue;
    }

    private void UpdateHex()
    {
        HexColor = $"#{Red:X2}{Green:X2}{Blue:X2}";
    }

    [RelayCommand]
    private void SetFromHex()
    {
        try
        {
            var rgb = _colorService.HexToRgb(HexColor);
            Red = rgb.R;
            Green = rgb.G;
            Blue = rgb.B;
        }
        catch
        {
            // Invalid hex color, ignore
        }
    }

    [RelayCommand]
    private void SetPresetColor(string colorName)
    {
        switch (colorName.ToLower())
        {
            case "red":
                Red = 255; Green = 0; Blue = 0;
                break;
            case "green":
                Red = 0; Green = 255; Blue = 0;
                break;
            case "blue":
                Red = 0; Green = 0; Blue = 255;
                break;
            case "yellow":
                Red = 255; Green = 255; Blue = 0;
                break;
            case "cyan":
                Red = 0; Green = 255; Blue = 255;
                break;
            case "magenta":
                Red = 255; Green = 0; Blue = 255;
                break;
            case "white":
                Red = 255; Green = 255; Blue = 255;
                break;
            case "black":
                Red = 0; Green = 0; Blue = 0;
                break;
        }
    }
}
