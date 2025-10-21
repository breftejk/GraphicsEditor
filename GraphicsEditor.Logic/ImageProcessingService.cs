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
    /// Sets the pan position directly.
    /// </summary>
    public void SetPan(double panX, double panY)
    {
        _panX = panX;
        _panY = panY;
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

    // ============================================
    // POINT TRANSFORMATIONS
    // ============================================

    /// <summary>
    /// Adds a value to all pixels in the image.
    /// </summary>
    public byte[] AddValue(int value)
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        
        byte[] result = new byte[_imageData.Length];
        for (int i = 0; i < _imageData.Length; i++)
        {
            int newValue = _imageData[i] + value;
            result[i] = (byte)Math.Clamp(newValue, 0, 255);
        }
        return result;
    }

    /// <summary>
    /// Subtracts a value from all pixels in the image.
    /// </summary>
    public byte[] SubtractValue(int value)
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        
        byte[] result = new byte[_imageData.Length];
        for (int i = 0; i < _imageData.Length; i++)
        {
            int newValue = _imageData[i] - value;
            result[i] = (byte)Math.Clamp(newValue, 0, 255);
        }
        return result;
    }

    /// <summary>
    /// Multiplies all pixels in the image by a value.
    /// </summary>
    public byte[] MultiplyValue(double value)
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        
        byte[] result = new byte[_imageData.Length];
        for (int i = 0; i < _imageData.Length; i++)
        {
            int newValue = (int)(_imageData[i] * value);
            result[i] = (byte)Math.Clamp(newValue, 0, 255);
        }
        return result;
    }

    /// <summary>
    /// Divides all pixels in the image by a value.
    /// </summary>
    public byte[] DivideValue(double value)
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        if (Math.Abs(value) < 0.001) throw new ArgumentException("Division by zero or near-zero value");
        
        byte[] result = new byte[_imageData.Length];
        for (int i = 0; i < _imageData.Length; i++)
        {
            int newValue = (int)(_imageData[i] / value);
            result[i] = (byte)Math.Clamp(newValue, 0, 255);
        }
        return result;
    }

    /// <summary>
    /// Changes the brightness of the image by the specified level.
    /// </summary>
    public byte[] ChangeBrightness(int level)
    {
        return AddValue(level);
    }

    /// <summary>
    /// Converts the image to grayscale using the average method.
    /// </summary>
    public byte[] ToGrayscaleAverage()
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        
        byte[] result = new byte[_imageData.Length];
        for (int i = 0; i < _imageData.Length; i += 3)
        {
            int avg = (_imageData[i] + _imageData[i + 1] + _imageData[i + 2]) / 3;
            result[i] = result[i + 1] = result[i + 2] = (byte)avg;
        }
        return result;
    }

    /// <summary>
    /// Converts the image to grayscale using the luminosity method (ITU-R BT.601 formula).
    /// </summary>
    public byte[] ToGrayscaleLuminosity()
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        
        byte[] result = new byte[_imageData.Length];
        for (int i = 0; i < _imageData.Length; i += 3)
        {
            int gray = (int)(0.299 * _imageData[i] + 0.587 * _imageData[i + 1] + 0.114 * _imageData[i + 2]);
            result[i] = result[i + 1] = result[i + 2] = (byte)Math.Clamp(gray, 0, 255);
        }
        return result;
    }

    // ============================================
    // IMAGE QUALITY ENHANCEMENT FILTERS
    // ============================================

    /// <summary>
    /// Applies a smoothing (averaging/box blur) filter.
    /// </summary>
    public byte[] ApplySmoothingFilter(int kernelSize = 3)
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        if (kernelSize % 2 == 0) throw new ArgumentException("Kernel size must be odd");
        
        byte[] result = new byte[_imageData.Length];
        int halfKernel = kernelSize / 2;
        
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                int sumR = 0, sumG = 0, sumB = 0;
                int count = 0;
                
                for (int ky = -halfKernel; ky <= halfKernel; ky++)
                {
                    for (int kx = -halfKernel; kx <= halfKernel; kx++)
                    {
                        int nx = x + kx;
                        int ny = y + ky;
                        
                        if (nx >= 0 && nx < _width && ny >= 0 && ny < _height)
                        {
                            int idx = (ny * _width + nx) * 3;
                            sumR += _imageData[idx];
                            sumG += _imageData[idx + 1];
                            sumB += _imageData[idx + 2];
                            count++;
                        }
                    }
                }
                
                int outIdx = (y * _width + x) * 3;
                result[outIdx] = (byte)(sumR / count);
                result[outIdx + 1] = (byte)(sumG / count);
                result[outIdx + 2] = (byte)(sumB / count);
            }
        }
        
        return result;
    }

    /// <summary>
    /// Applies a median filter to remove impulse noise.
    /// </summary>
    public byte[] ApplyMedianFilter(int kernelSize = 3)
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        if (kernelSize % 2 == 0) throw new ArgumentException("Kernel size must be odd");
        
        byte[] result = new byte[_imageData.Length];
        int halfKernel = kernelSize / 2;
        List<byte> windowR = new List<byte>();
        List<byte> windowG = new List<byte>();
        List<byte> windowB = new List<byte>();
        
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                windowR.Clear();
                windowG.Clear();
                windowB.Clear();
                
                for (int ky = -halfKernel; ky <= halfKernel; ky++)
                {
                    for (int kx = -halfKernel; kx <= halfKernel; kx++)
                    {
                        int nx = x + kx;
                        int ny = y + ky;
                        
                        if (nx >= 0 && nx < _width && ny >= 0 && ny < _height)
                        {
                            int idx = (ny * _width + nx) * 3;
                            windowR.Add(_imageData[idx]);
                            windowG.Add(_imageData[idx + 1]);
                            windowB.Add(_imageData[idx + 2]);
                        }
                    }
                }
                
                windowR.Sort();
                windowG.Sort();
                windowB.Sort();
                
                int outIdx = (y * _width + x) * 3;
                result[outIdx] = windowR[windowR.Count / 2];
                result[outIdx + 1] = windowG[windowG.Count / 2];
                result[outIdx + 2] = windowB[windowB.Count / 2];
            }
        }
        
        return result;
    }

    /// <summary>
    /// Applies Sobel edge detection filter.
    /// </summary>
    public byte[] ApplySobelFilter()
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        
        // Sobel kernels
        int[,] sobelX = { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
        int[,] sobelY = { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };
        
        byte[] result = new byte[_imageData.Length];
        
        for (int y = 1; y < _height - 1; y++)
        {
            for (int x = 1; x < _width - 1; x++)
            {
                int gxR = 0, gyR = 0;
                int gxG = 0, gyG = 0;
                int gxB = 0, gyB = 0;
                
                for (int ky = -1; ky <= 1; ky++)
                {
                    for (int kx = -1; kx <= 1; kx++)
                    {
                        int idx = ((y + ky) * _width + (x + kx)) * 3;
                        
                        gxR += _imageData[idx] * sobelX[ky + 1, kx + 1];
                        gyR += _imageData[idx] * sobelY[ky + 1, kx + 1];
                        
                        gxG += _imageData[idx + 1] * sobelX[ky + 1, kx + 1];
                        gyG += _imageData[idx + 1] * sobelY[ky + 1, kx + 1];
                        
                        gxB += _imageData[idx + 2] * sobelX[ky + 1, kx + 1];
                        gyB += _imageData[idx + 2] * sobelY[ky + 1, kx + 1];
                    }
                }
                
                int magnitudeR = (int)Math.Sqrt(gxR * gxR + gyR * gyR);
                int magnitudeG = (int)Math.Sqrt(gxG * gxG + gyG * gyG);
                int magnitudeB = (int)Math.Sqrt(gxB * gxB + gyB * gyB);
                
                int outIdx = (y * _width + x) * 3;
                result[outIdx] = (byte)Math.Clamp(magnitudeR, 0, 255);
                result[outIdx + 1] = (byte)Math.Clamp(magnitudeG, 0, 255);
                result[outIdx + 2] = (byte)Math.Clamp(magnitudeB, 0, 255);
            }
        }
        
        return result;
    }

    /// <summary>
    /// Applies a high-pass sharpening filter.
    /// </summary>
    public byte[] ApplySharpeningFilter()
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        
        // Sharpening kernel
        double[,] kernel = {
            { 0, -1, 0 },
            { -1, 5, -1 },
            { 0, -1, 0 }
        };
        
        return ApplyConvolution(kernel);
    }

    /// <summary>
    /// Applies Gaussian blur filter.
    /// </summary>
    public byte[] ApplyGaussianBlur(double sigma = 1.0)
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        
        // Generate Gaussian kernel (5x5)
        int kernelSize = 5;
        double[,] kernel = GenerateGaussianKernel(kernelSize, sigma);
        
        return ApplyConvolution(kernel);
    }

    /// <summary>
    /// Applies convolution with a custom kernel of any size and values.
    /// </summary>
    public byte[] ApplyConvolution(double[,] kernel)
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        
        int kernelHeight = kernel.GetLength(0);
        int kernelWidth = kernel.GetLength(1);
        
        if (kernelHeight % 2 == 0 || kernelWidth % 2 == 0)
            throw new ArgumentException("Kernel dimensions must be odd");
        
        byte[] result = new byte[_imageData.Length];
        int halfKernelY = kernelHeight / 2;
        int halfKernelX = kernelWidth / 2;
        
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                double sumR = 0, sumG = 0, sumB = 0;
                
                for (int ky = 0; ky < kernelHeight; ky++)
                {
                    for (int kx = 0; kx < kernelWidth; kx++)
                    {
                        int nx = x + kx - halfKernelX;
                        int ny = y + ky - halfKernelY;
                        
                        // Handle borders by clamping
                        nx = Math.Clamp(nx, 0, _width - 1);
                        ny = Math.Clamp(ny, 0, _height - 1);
                        
                        int idx = (ny * _width + nx) * 3;
                        double kernelValue = kernel[ky, kx];
                        
                        sumR += _imageData[idx] * kernelValue;
                        sumG += _imageData[idx + 1] * kernelValue;
                        sumB += _imageData[idx + 2] * kernelValue;
                    }
                }
                
                int outIdx = (y * _width + x) * 3;
                result[outIdx] = (byte)Math.Clamp((int)sumR, 0, 255);
                result[outIdx + 1] = (byte)Math.Clamp((int)sumG, 0, 255);
                result[outIdx + 2] = (byte)Math.Clamp((int)sumB, 0, 255);
            }
        }
        
        return result;
    }

    /// <summary>
    /// Generates a Gaussian kernel for blur operations.
    /// </summary>
    private double[,] GenerateGaussianKernel(int size, double sigma)
    {
        double[,] kernel = new double[size, size];
        double sum = 0;
        int halfSize = size / 2;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dy = y - halfSize;
                int dx = x - halfSize;
                kernel[y, x] = Math.Exp(-(dx * dx + dy * dy) / (2 * sigma * sigma));
                sum += kernel[y, x];
            }
        }
        
        // Normalize
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                kernel[y, x] /= sum;
            }
        }
        
        return kernel;
    }

    /// <summary>
    /// Updates the current image data with new processed data.
    /// </summary>
    public void UpdateImageData(byte[] newData)
    {
        if (newData.Length != _imageData?.Length)
            throw new ArgumentException("New data must have the same length as current image data");
        
        _imageData = newData;
    }
}

