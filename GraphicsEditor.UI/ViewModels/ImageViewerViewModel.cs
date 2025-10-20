using System;
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

    [ObservableProperty]
    private byte[]? _imageData;
    
    // Store original image data (before JPEG compression)
    private byte[]? _originalImageData;

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

    [ObservableProperty]
    private bool _isImageLoaded = false;

    [ObservableProperty]
    private string _errorMessage = "";

    public ImageViewerViewModel()
    {
        _imageService = new ImageProcessingService();
        _ppmReader = new PpmReader();
        _ppmWriter = new PpmWriter();
        _jpegReader = new JpegReader();
        _jpegWriter = new JpegWriter();
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
        // Always save from original data, not the preview
        var dataToSave = _originalImageData ?? ImageData;
        
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
}
