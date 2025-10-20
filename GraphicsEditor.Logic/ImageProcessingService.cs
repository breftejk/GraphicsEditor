using GraphicsEditor.Core.Models;

namespace GraphicsEditor.Logic;

/// <summary>
/// Provides image processing operations like zoom, pan, and pixel inspection.
/// </summary>
public class ImageProcessingService
{
    private byte[]? _imageData;
    private int _width;
    private int _height;
    private double _zoomLevel = 1.0;
    private double _panX = 0;
    private double _panY = 0;

    public int Width => _width;
    public int Height => _height;
    public double ZoomLevel => _zoomLevel;
    public double PanX => _panX;
    public double PanY => _panY;

    /// <summary>
    /// Loads image data for processing.
    /// </summary>
    public void LoadImage(byte[] imageData, int width, int height)
    {
        _imageData = imageData;
        _width = width;
        _height = height;
        ResetViewport();
    }

    /// <summary>
    /// Resets zoom and pan to default values.
    /// </summary>
    public void ResetViewport()
    {
        _zoomLevel = 1.0;
        _panX = 0;
        _panY = 0;
    }

    /// <summary>
    /// Zooms in by a factor.
    /// </summary>
    public void ZoomIn(double factor = 1.2)
    {
        _zoomLevel *= factor;
        _zoomLevel = Math.Min(_zoomLevel, 500.0); // Max zoom - allow extreme magnification for pixel inspection
    }

    /// <summary>
    /// Zooms out by a factor.
    /// </summary>
    public void ZoomOut(double factor = 1.2)
    {
        _zoomLevel /= factor;
        _zoomLevel = Math.Max(_zoomLevel, 0.1); // Min zoom
    }

    /// <summary>
    /// Sets a specific zoom level.
    /// </summary>
    public void SetZoom(double zoom)
    {
        _zoomLevel = Math.Clamp(zoom, 0.1, 500.0);
    }

    /// <summary>
    /// Pans the viewport by the specified offset.
    /// </summary>
    public void Pan(double deltaX, double deltaY)
    {
        _panX += deltaX;
        _panY += deltaY;
    }

    /// <summary>
    /// Gets the RGB color at the specified pixel coordinates.
    /// </summary>
    public RgbColor? GetPixelColor(int x, int y)
    {
        if (_imageData == null || x < 0 || x >= _width || y < 0 || y >= _height)
            return null;

        int index = (y * _width + x) * 3; // Assuming RGB format
        if (index + 2 >= _imageData.Length)
            return null;

        return new RgbColor(_imageData[index], _imageData[index + 1], _imageData[index + 2]);
    }

    /// <summary>
    /// Converts screen coordinates to image coordinates based on current zoom and pan.
    /// </summary>
    public (int x, int y)? ScreenToImageCoordinates(double screenX, double screenY, double viewportWidth, double viewportHeight)
    {
        // Adjust for pan and zoom
        double imageX = (screenX - _panX) / _zoomLevel;
        double imageY = (screenY - _panY) / _zoomLevel;

        int x = (int)imageX;
        int y = (int)imageY;

        if (x < 0 || x >= _width || y < 0 || y >= _height)
            return null;

        return (x, y);
    }

    /// <summary>
    /// Gets the visible region of the image based on viewport size.
    /// </summary>
    public (int x, int y, int width, int height) GetVisibleRegion(double viewportWidth, double viewportHeight)
    {
        int x = Math.Max(0, (int)(-_panX / _zoomLevel));
        int y = Math.Max(0, (int)(-_panY / _zoomLevel));
        int width = Math.Min(_width - x, (int)(viewportWidth / _zoomLevel));
        int height = Math.Min(_height - y, (int)(viewportHeight / _zoomLevel));

        return (x, y, width, height);
    }

    /// <summary>
    /// Clears the currently loaded image.
    /// </summary>
    public void ClearImage()
    {
        _imageData = null;
        _width = 0;
        _height = 0;
        ResetViewport();
    }
}
