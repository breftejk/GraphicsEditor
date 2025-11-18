using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraphicsEditor.Core.Models;
using GraphicsEditor.IO;
using GraphicsEditor.Logic;

namespace GraphicsEditor.UI.ViewModels;

/// <summary>
/// View model for the image viewer tab.
/// Supports loading PPM and JPEG files, zoom, pan, and pixel inspection.
/// </summary>
public partial class ImageViewerViewModel : ViewModelBase
{
    private readonly ImageProcessingService _imageService;
    private readonly PpmReader _ppmReader;
    private readonly PpmWriter _ppmWriter;
    private readonly JpegReader _jpegReader;
    private readonly JpegWriter _jpegWriter;
    private readonly PngReader _pngReader;
    private readonly PngWriter _pngWriter;

    [ObservableProperty]
    private byte[]? _imageData;
    
    // Store original image data (before any processing)
    private byte[]? _originalImageData;
    
    // Stack to store image history for undo functionality
    private Stack<byte[]> _imageHistory = new Stack<byte[]>();
    
    // Can undo flag
    [ObservableProperty]
    private bool _canUndo = false;

    [ObservableProperty]
    private int _imageWidth;

    [ObservableProperty]
    private int _imageHeight;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    partial void OnZoomLevelChanged(double value)
    {
        // Ensure zoom level is within valid range
        if (value < 0.1)
        {
            ZoomLevel = 0.1;
        }
        else if (value > 500.0)
        {
            ZoomLevel = 500.0;
        }
    }

    partial void OnSelectedPixelColorChanged(RgbColor? value)
    {
        OnPropertyChanged(nameof(SelectedPixelColorBrush));
    }

    [ObservableProperty]
    private string _loadedFilePath = "";

    [ObservableProperty]
    private string _statusMessage = "No image loaded";

    [ObservableProperty]
    private int _jpegQuality = 100;

    [ObservableProperty]
    private RgbColor? _selectedPixelColor;

    [ObservableProperty]
    private string _pixelInfo = "";
    
    // Computed property for pixel color brush
    public Avalonia.Media.SolidColorBrush? SelectedPixelColorBrush
    {
        get
        {
            if (SelectedPixelColor.HasValue)
            {
                var color = SelectedPixelColor.Value;
                return new Avalonia.Media.SolidColorBrush(
                    Avalonia.Media.Color.FromRgb(color.R, color.G, color.B));
            }
            return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Transparent);
        }
    }

    [ObservableProperty]
    private bool _isImageLoaded = false;

    [ObservableProperty]
    private string _errorMessage = "";

    // Point transformation parameters
    [ObservableProperty]
    private int _addValue = 0;

    [ObservableProperty]
    private int _subtractValue = 0;

    [ObservableProperty]
    private double _multiplyValue = 1.0;

    [ObservableProperty]
    private double _divideValue = 1.0;

    [ObservableProperty]
    private int _brightnessLevel = 0;

    // Filter parameters
    [ObservableProperty]
    private int _smoothingKernelSize = 3;

    [ObservableProperty]
    private int _medianKernelSize = 3;

    [ObservableProperty]
    private double _gaussianSigma = 1.0;

    [ObservableProperty]
    private string _customKernelInput = "0,-1,0\n-1,5,-1\n0,-1,0";

    // Binarization parameters
    [ObservableProperty]
    private int _manualThreshold = 128;

    [ObservableProperty]
    private double _percentBlack = 50.0;

    // Histogram data for visualization
    [ObservableProperty]
    private int[]? _histogramData;

    [ObservableProperty]
    private bool _hasHistogramData = false;

    public ImageViewerViewModel()
    {
        _imageService = new ImageProcessingService();
        _ppmReader = new PpmReader();
        _ppmWriter = new PpmWriter();
        _jpegReader = new JpegReader();
        _jpegWriter = new JpegWriter();
        _pngReader = new PngReader();
        _pngWriter = new PngWriter();
    }

    partial void OnJpegQualityChanged(int value)
    {
        UpdateJpegPreview();
    }

    private void UpdateJpegPreview()
    {
        if (_originalImageData == null || ImageWidth == 0 || ImageHeight == 0)
            return;

        if (JpegQuality == 100)
        {
            // Show original at 100%
            ImageData = _originalImageData;
            StatusMessage = $"Showing original image (100% quality) - {ImageWidth}x{ImageHeight}";
        }
        else
        {
            // Compress to JPEG and reload to show artifacts
            try
            {
                var tempFile = Path.GetTempFileName() + ".jpg";
                _jpegWriter.Write(tempFile, _originalImageData, ImageWidth, ImageHeight, JpegQuality);
                var compressedImage = _jpegReader.Read(tempFile);
                ImageData = compressedImage.PixelData;
                
                // Clean up temp file
                try { File.Delete(tempFile); } catch { }
                
                var fileInfo = new FileInfo(tempFile);
                StatusMessage = $"Preview with {JpegQuality}% quality - {ImageWidth}x{ImageHeight}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error generating preview: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void LoadImageFromPath(string filePath)
    {
        try
        {
            FileErrorHandler.ValidateFileExists(filePath);

            var format = FileErrorHandler.DetectImageFormat(filePath);

            if (format == ImageFormat.Ppm)
            {
                var ppmImage = _ppmReader.Read(filePath);
                ImageData = ppmImage.PixelData;
                _originalImageData = (byte[])ppmImage.PixelData.Clone(); // Store original
                ImageWidth = ppmImage.Width;
                ImageHeight = ppmImage.Height;
                StatusMessage = $"Loaded PPM {ppmImage.Format}: {ImageWidth}x{ImageHeight}";
            }
            else if (format == ImageFormat.Jpeg)
            {
                var jpegImage = _jpegReader.Read(filePath);
                ImageData = jpegImage.PixelData;
                _originalImageData = (byte[])jpegImage.PixelData.Clone(); // Store original
                ImageWidth = jpegImage.Width;
                ImageHeight = jpegImage.Height;
                StatusMessage = $"Loaded JPEG: {ImageWidth}x{ImageHeight}";
            }
            else if (format == ImageFormat.Png)
            {
                var pngImage = _pngReader.ReadAsRgb(filePath);
                ImageData = pngImage.PixelData;
                _originalImageData = (byte[])pngImage.PixelData.Clone(); // Store original
                ImageWidth = pngImage.Width;
                ImageHeight = pngImage.Height;
                StatusMessage = $"Loaded PNG: {ImageWidth}x{ImageHeight}";
            }

            _imageService.LoadImage(ImageData!, ImageWidth, ImageHeight);
            LoadedFilePath = filePath;
            ZoomLevel = 1.0;
        }
        catch (Exception ex)
        {
            StatusMessage = FileErrorHandler.GetUserFriendlyMessage(ex);
        }
    }

    // Public async method for View to call
    public async Task LoadImageFromPathAsync(string filePath)
    {
        try
        {
            StatusMessage = "Loading image...";
            ErrorMessage = "";
            
            // Load image data on background thread
            var imageData = await Task.Run(() =>
            {
                FileErrorHandler.ValidateFileExists(filePath);

                var format = FileErrorHandler.DetectImageFormat(filePath);

                if (format == ImageFormat.Ppm)
                {
                    var ppmImage = _ppmReader.Read(filePath);
                    return (ppmImage.PixelData, ppmImage.Width, ppmImage.Height, $"Loaded PPM {ppmImage.Format}: {ppmImage.Width}x{ppmImage.Height}");
                }
                else if (format == ImageFormat.Jpeg)
                {
                    var jpegImage = _jpegReader.Read(filePath);
                    return (jpegImage.PixelData, jpegImage.Width, jpegImage.Height, $"Loaded JPEG: {jpegImage.Width}x{jpegImage.Height}");
                }
                else if (format == ImageFormat.Png)
                {
                    var pngImage = _pngReader.ReadAsRgb(filePath);
                    return (pngImage.PixelData, pngImage.Width, pngImage.Height, $"Loaded PNG: {pngImage.Width}x{pngImage.Height}");
                }
                
                throw new InvalidOperationException("Unsupported image format");
            });
            
            // Update properties on UI thread
            ImageData = imageData.PixelData;
            _originalImageData = (byte[])imageData.PixelData.Clone();
            ImageWidth = imageData.Width;
            ImageHeight = imageData.Height;
            StatusMessage = imageData.Item4;
            ErrorMessage = "";
            
            _imageService.LoadImage(ImageData!, ImageWidth, ImageHeight);
            LoadedFilePath = filePath;
            ZoomLevel = 1.0;
            IsImageLoaded = true;
            
            // Reset history and filters
            _imageHistory.Clear();
            CanUndo = false;
            
            // Reset all filters and transformations to default values
            ResetAllFiltersAndTransformations();
            
            // Update histogram
            UpdateHistogram();
        }
        catch (Exception ex)
        {
            ErrorMessage = FileErrorHandler.GetUserFriendlyMessage(ex);
            StatusMessage = "";
            IsImageLoaded = false;
        }
    }

    [RelayCommand]
    private void SaveImageAsJpeg(string filePath)
    {
        // Save the current (possibly processed) image data
        var dataToSave = ImageData;
        
        if (dataToSave == null)
        {
            StatusMessage = "No image to save";
            return;
        }

        try
        {
            FileErrorHandler.ValidateFileWritable(filePath);
            _jpegWriter.Write(filePath, dataToSave, ImageWidth, ImageHeight, JpegQuality);
            StatusMessage = $"Saved JPEG with quality {JpegQuality}% to {filePath}";
        }
        catch (Exception ex)
        {
            StatusMessage = FileErrorHandler.GetUserFriendlyMessage(ex);
        }
    }

    [RelayCommand]
    private void SaveImageAsPng(string filePath)
    {
        // Save the current (possibly processed) image data
        var dataToSave = ImageData;
        
        if (dataToSave == null)
        {
            StatusMessage = "No image to save";
            return;
        }

        try
        {
            FileErrorHandler.ValidateFileWritable(filePath);
            _pngWriter.WriteRgb(filePath, dataToSave, ImageWidth, ImageHeight);
            StatusMessage = $"Saved PNG to {filePath}";
        }
        catch (Exception ex)
        {
            StatusMessage = FileErrorHandler.GetUserFriendlyMessage(ex);
        }
    }

    [RelayCommand]
    private void ZoomIn()
    {
        _imageService.ZoomIn();
        ZoomLevel = _imageService.ZoomLevel;
        StatusMessage = $"Zoom: {ZoomLevel:F2}x";
    }

    [RelayCommand]
    private void ZoomOut()
    {
        _imageService.ZoomOut();
        ZoomLevel = _imageService.ZoomLevel;
        StatusMessage = $"Zoom: {ZoomLevel:F2}x";
    }

    [RelayCommand]
    private void ResetZoom()
    {
        _imageService.ResetViewport();
        ZoomLevel = _imageService.ZoomLevel;
        StatusMessage = "Zoom reset to 100%";
    }

    /// <summary>
    /// Handles mouse click on image to inspect pixel color.
    /// </summary>
    public void OnImageClick(double x, double y, double viewportWidth, double viewportHeight)
    {
        var coords = _imageService.ScreenToImageCoordinates(x, y, viewportWidth, viewportHeight);
        if (coords.HasValue)
        {
            var color = _imageService.GetPixelColor(coords.Value.x, coords.Value.y);
            if (color.HasValue)
            {
                SelectedPixelColor = color.Value;
                PixelInfo = $"Pixel ({coords.Value.x}, {coords.Value.y}): {color.Value}";
            }
        }
    }

    /// <summary>
    /// Handles panning the image.
    /// </summary>
    public void OnPan(double deltaX, double deltaY)
    {
        _imageService.Pan(deltaX, deltaY);
    }

    // ============================================
    // POINT TRANSFORMATIONS
    // ============================================

    [RelayCommand]
    private void ApplyAdd()
    {
        if (!IsImageLoaded || ImageData == null) return;
        
        try
        {
            // Use current image data without resetting viewport
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.AddValue(AddValue);
            ApplyProcessedImage(result, $"Add: {AddValue}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplySubtract()
    {
        if (!IsImageLoaded || ImageData == null) return;
        
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.SubtractValue(SubtractValue);
            ApplyProcessedImage(result, $"Subtract: {SubtractValue}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplyMultiply()
    {
        if (!IsImageLoaded || ImageData == null) return;
        
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.MultiplyValue(MultiplyValue);
            ApplyProcessedImage(result, $"Multiply: {MultiplyValue:F2}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplyDivide()
    {
        if (!IsImageLoaded || ImageData == null) return;
        
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.DivideValue(DivideValue);
            ApplyProcessedImage(result, $"Divide: {DivideValue:F2}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplyBrightness()
    {
        if (!IsImageLoaded || ImageData == null) return;
        
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.ChangeBrightness(BrightnessLevel);
            ApplyProcessedImage(result, $"Brightness: {BrightnessLevel}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplyGrayscaleAverage()
    {
        if (!IsImageLoaded || ImageData == null) return;
        
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.ToGrayscaleAverage();
            ApplyProcessedImage(result, "Grayscale (Average)");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplyGrayscaleLuminosity()
    {
        if (!IsImageLoaded || ImageData == null) return;
        
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.ToGrayscaleLuminosity();
            ApplyProcessedImage(result, "Grayscale (Luminosity)");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    // ============================================
    // IMAGE QUALITY ENHANCEMENT FILTERS
    // ============================================

    [RelayCommand]
    private void ApplySmoothingFilter()
    {
        if (!IsImageLoaded || ImageData == null) return;
        
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.ApplySmoothingFilter(SmoothingKernelSize);
            ApplyProcessedImage(result, $"Smoothing (kernel: {SmoothingKernelSize})");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplyMedianFilter()
    {
        if (!IsImageLoaded || ImageData == null) return;
        
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.ApplyMedianFilter(MedianKernelSize);
            ApplyProcessedImage(result, $"Median (kernel: {MedianKernelSize})");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplySobelFilter()
    {
        if (!IsImageLoaded || ImageData == null) return;
        
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.ApplySobelFilter();
            ApplyProcessedImage(result, "Sobel Edge Detection");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplySharpeningFilter()
    {
        if (!IsImageLoaded || ImageData == null) return;
        
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.ApplySharpeningFilter();
            ApplyProcessedImage(result, "Sharpening");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplyGaussianBlur()
    {
        if (!IsImageLoaded || ImageData == null) return;
        
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.ApplyGaussianBlur(GaussianSigma);
            ApplyProcessedImage(result, $"Gaussian Blur (sigma: {GaussianSigma:F2})");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplyCustomKernel()
    {
        if (!IsImageLoaded || ImageData == null) return;
        
        try
        {
            _imageService.UpdateImageData(ImageData);
            var kernel = ParseCustomKernel(CustomKernelInput);
            var result = _imageService.ApplyConvolution(kernel);
            ApplyProcessedImage(result, $"Custom Kernel ({kernel.GetLength(0)}x{kernel.GetLength(1)})");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    // ============================================
    // HISTOGRAM
    // ============================================
    [RelayCommand]
    private void ApplyHistogramStretching()
    {
        if (!IsImageLoaded || ImageData == null) return;
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.HistogramStretching();
            ApplyProcessedImage(result, "Histogram Stretching");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplyHistogramEqualization()
    {
        if (!IsImageLoaded || ImageData == null) return;
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.HistogramEqualization();
            ApplyProcessedImage(result, "Histogram Equalization");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    // ============================================
    // BINARIZATION
    // ============================================
    [RelayCommand]
    private void ApplyBinarizeManual()
    {
        if (!IsImageLoaded || ImageData == null) return;
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.BinarizeManual(ManualThreshold);
            ApplyProcessedImage(result, $"Binarization (Manual: {ManualThreshold})");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplyBinarizePercentBlack()
    {
        if (!IsImageLoaded || ImageData == null) return;
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.BinarizePercentBlack(PercentBlack);
            ApplyProcessedImage(result, $"Binarization (Percent Black: {PercentBlack:F1}%)");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplyBinarizeMeanIterative()
    {
        if (!IsImageLoaded || ImageData == null) return;
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.BinarizeMeanIterative();
            ApplyProcessedImage(result, "Binarization (Mean Iterative Selection)");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplyBinarizeEntropy()
    {
        if (!IsImageLoaded || ImageData == null) return;
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.BinarizeEntropy();
            ApplyProcessedImage(result, "Binarization (Entropy Selection)");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplyBinarizeMinimumError()
    {
        if (!IsImageLoaded || ImageData == null) return;
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.BinarizeMinimumError();
            ApplyProcessedImage(result, "Binarization (Minimum Error)");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplyBinarizeFuzzyMinimumError()
    {
        if (!IsImageLoaded || ImageData == null) return;
        try
        {
            _imageService.UpdateImageData(ImageData);
            var result = _imageService.BinarizeFuzzyMinimumError();
            ApplyProcessedImage(result, "Binarization (Fuzzy Minimum Error)");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ResetToOriginal()
    {
        if (_originalImageData != null)
        {
            // Preserve zoom and pan
            var currentZoom = _imageService.ZoomLevel;
            var currentPanX = _imageService.PanX;
            var currentPanY = _imageService.PanY;

            ImageData = (byte[])_originalImageData.Clone();
            _imageService.UpdateImageData(ImageData);

            // Restore viewport
            _imageService.SetZoom(currentZoom);
            _imageService.SetPan(currentPanX, currentPanY);
            ZoomLevel = currentZoom;

            StatusMessage = "Reset to original";
            
            // Clear history when resetting to original
            _imageHistory.Clear();
            CanUndo = false;
            
            // Reset all parameters to default
            ResetAllFiltersAndTransformations();

            // Update histogram
            UpdateHistogram();
        }
    }

    [RelayCommand]
    private void UndoLastFilter()
    {
        if (_imageHistory.Count > 0)
        {
            // Preserve zoom and pan
            var currentZoom = _imageService.ZoomLevel;
            var currentPanX = _imageService.PanX;
            var currentPanY = _imageService.PanY;

            // Restore previous image from history
            ImageData = _imageHistory.Pop();
            _imageService.UpdateImageData(ImageData);
            
            // Restore viewport
            _imageService.SetZoom(currentZoom);
            _imageService.SetPan(currentPanX, currentPanY);
            ZoomLevel = currentZoom;

            CanUndo = _imageHistory.Count > 0;
            StatusMessage = $"Undo applied. History: {_imageHistory.Count} steps";

            // Update histogram
            UpdateHistogram();
        }
    }

    /// <summary>
    /// Resets all filter and transformation parameters to their default values.
    /// </summary>
    private void ResetAllFiltersAndTransformations()
    {
        // Point transformation parameters
        AddValue = 0;
        SubtractValue = 0;
        MultiplyValue = 1.0;
        DivideValue = 1.0;
        BrightnessLevel = 0;
        
        // Filter parameters
        SmoothingKernelSize = 3;
        MedianKernelSize = 3;
        GaussianSigma = 1.0;
        CustomKernelInput = "0,-1,0\n-1,5,-1\n0,-1,0";
        
        // Binarization parameters
        ManualThreshold = 128;
        PercentBlack = 50.0;
        
        // JPEG quality
        JpegQuality = 100;
    }

    /// <summary>
    /// Applies processed image data to the current view.
    /// Saves current state to history for undo functionality.
    /// </summary>
    private void ApplyProcessedImage(byte[] processedData, string message)
    {
        // Save current image to history before applying new one
        if (ImageData != null)
        {
            _imageHistory.Push((byte[])ImageData.Clone());
            CanUndo = true;
        }
        
        // Save current zoom and pan before updating
        var currentZoom = _imageService.ZoomLevel;
        var currentPanX = _imageService.PanX;
        var currentPanY = _imageService.PanY;
        
        ImageData = processedData;
        _imageService.UpdateImageData(processedData);
        
        // Restore zoom and pan after updating
        _imageService.SetZoom(currentZoom);
        _imageService.SetPan(currentPanX, currentPanY);
        ZoomLevel = currentZoom;
        
        StatusMessage = message + $" (History: {_imageHistory.Count} steps)";
        
        // Update histogram after processing
        UpdateHistogram();
    }

    /// <summary>
    /// Updates the histogram data from current image.
    /// </summary>
    private void UpdateHistogram()
    {
        if (ImageData == null || ImageWidth == 0 || ImageHeight == 0)
        {
            HistogramData = null;
            HasHistogramData = false;
            return;
        }

        try
        {
            // Do NOT call LoadImage here (it resets zoom/pan). Just compute the histogram from current data.
            HistogramData = _imageService.CalculateHistogram(ImageData, -1); // -1 for grayscale average
            HasHistogramData = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error calculating histogram: {ex.Message}";
            HasHistogramData = false;
        }
    }

    /// <summary>
    /// Parses custom kernel input from text format.
    /// Format: comma-separated values, rows separated by newlines.
    /// Example: "0,-1,0\n-1,5,-1\n0,-1,0"
    /// </summary>
    private double[,] ParseCustomKernel(string input)
    {
        var lines = input.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
            throw new ArgumentException("Kernel input is empty");

        var rows = lines.Length;
        var cols = lines[0].Split(',').Length;

        if (rows % 2 == 0 || cols % 2 == 0)
            throw new ArgumentException("Kernel dimensions must be odd");

        double[,] kernel = new double[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            var values = lines[i].Split(',');
            if (values.Length != cols)
                throw new ArgumentException("All rows must have the same number of columns");

            for (int j = 0; j < cols; j++)
            {
                if (!double.TryParse(values[j].Trim(), out double value))
                    throw new ArgumentException($"Invalid number format: {values[j]}");
                kernel[i, j] = value;
            }
        }

        return kernel;
    }
}
