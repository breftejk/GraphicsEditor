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
    /// Applies Gaussian blur filter with optimal kernel size based on sigma.
    /// Optimization: Uses larger kernel for larger sigma values for better quality.
    /// </summary>
    public byte[] ApplyGaussianBlur(double sigma = 1.0)
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        
        // Optimization: Calculate optimal kernel size based on sigma
        // Rule of thumb: kernel size should be at least 6*sigma + 1 to capture 99.7% of the Gaussian
        // We use ceiling to ensure odd size and limit to reasonable maximum
        int kernelSize = Math.Min((int)Math.Ceiling(sigma * 6) | 1, 31); // Ensure odd, max 31x31
        if (kernelSize < 3) kernelSize = 3; // Minimum 3x3
        
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

    // ============================================
    // HISTOGRAM OPERATIONS
    // ============================================

    /// <summary>
    /// Calculates histogram for grayscale image or each RGB channel.
    /// Returns array of 256 values for each intensity level.
    /// </summary>
    public int[] CalculateHistogram(byte[] imageData, int channel = -1)
    {
        int[] histogram = new int[256];
        
        if (channel == -1)
        {
            // All channels (average for grayscale)
            for (int i = 0; i < imageData.Length; i += 3)
            {
                int gray = (imageData[i] + imageData[i + 1] + imageData[i + 2]) / 3;
                histogram[gray]++;
            }
        }
        else
        {
            // Specific channel (0=R, 1=G, 2=B)
            for (int i = channel; i < imageData.Length; i += 3)
            {
                histogram[imageData[i]]++;
            }
        }
        
        return histogram;
    }

    /// <summary>
    /// Applies histogram stretching (normalization) to expand the range of intensities.
    /// </summary>
    public byte[] HistogramStretching()
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        
        byte[] result = new byte[_imageData.Length];
        
        // Find min and max values for each channel
        byte minR = 255, maxR = 0;
        byte minG = 255, maxG = 0;
        byte minB = 255, maxB = 0;
        
        for (int i = 0; i < _imageData.Length; i += 3)
        {
            if (_imageData[i] < minR) minR = _imageData[i];
            if (_imageData[i] > maxR) maxR = _imageData[i];
            
            if (_imageData[i + 1] < minG) minG = _imageData[i + 1];
            if (_imageData[i + 1] > maxG) maxG = _imageData[i + 1];
            
            if (_imageData[i + 2] < minB) minB = _imageData[i + 2];
            if (_imageData[i + 2] > maxB) maxB = _imageData[i + 2];
        }
        
        // Stretch each channel independently
        double rangeR = maxR - minR;
        double rangeG = maxG - minG;
        double rangeB = maxB - minB;
        
        for (int i = 0; i < _imageData.Length; i += 3)
        {
            if (rangeR > 0)
                result[i] = (byte)((_imageData[i] - minR) * 255.0 / rangeR);
            else
                result[i] = _imageData[i];
            
            if (rangeG > 0)
                result[i + 1] = (byte)((_imageData[i + 1] - minG) * 255.0 / rangeG);
            else
                result[i + 1] = _imageData[i + 1];
            
            if (rangeB > 0)
                result[i + 2] = (byte)((_imageData[i + 2] - minB) * 255.0 / rangeB);
            else
                result[i + 2] = _imageData[i + 2];
        }
        
        return result;
    }

    /// <summary>
    /// Applies histogram equalization to improve contrast.
    /// </summary>
    public byte[] HistogramEqualization()
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        
        byte[] result = new byte[_imageData.Length];
        
        // Process each channel separately
        for (int channel = 0; channel < 3; channel++)
        {
            // Calculate histogram for this channel
            int[] histogram = new int[256];
            for (int i = channel; i < _imageData.Length; i += 3)
            {
                histogram[_imageData[i]]++;
            }
            
            // Calculate cumulative distribution function (CDF)
            int[] cdf = new int[256];
            cdf[0] = histogram[0];
            for (int i = 1; i < 256; i++)
            {
                cdf[i] = cdf[i - 1] + histogram[i];
            }
            
            // Find minimum non-zero CDF value
            int cdfMin = 0;
            for (int i = 0; i < 256; i++)
            {
                if (cdf[i] > 0)
                {
                    cdfMin = cdf[i];
                    break;
                }
            }
            
            // Calculate total number of pixels for this channel
            int totalPixels = _width * _height;
            
            // Apply equalization using the formula: h(v) = round((cdf(v) - cdf_min) / (M*N - cdf_min) * 255)
            byte[] lookupTable = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                if (totalPixels - cdfMin > 0)
                    lookupTable[i] = (byte)Math.Round((cdf[i] - cdfMin) * 255.0 / (totalPixels - cdfMin));
                else
                    lookupTable[i] = (byte)i;
            }
            
            // Apply lookup table
            for (int i = channel; i < _imageData.Length; i += 3)
            {
                result[i] = lookupTable[_imageData[i]];
            }
        }
        
        return result;
    }

    // ============================================
    // BINARIZATION METHODS
    // ============================================

    /// <summary>
    /// Applies manual thresholding binarization.
    /// Pixels above threshold become white (255), below become black (0).
    /// </summary>
    public byte[] BinarizeManual(int threshold)
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        
        byte[] result = new byte[_imageData.Length];
        
        for (int i = 0; i < _imageData.Length; i += 3)
        {
            // Convert to grayscale first
            int gray = (int)(0.299 * _imageData[i] + 0.587 * _imageData[i + 1] + 0.114 * _imageData[i + 2]);
            
            byte value = (byte)(gray >= threshold ? 255 : 0);
            result[i] = result[i + 1] = result[i + 2] = value;
        }
        
        return result;
    }

    /// <summary>
    /// Applies Percent Black Selection thresholding.
    /// Selects threshold so that a specified percentage of pixels become black.
    /// </summary>
    public byte[] BinarizePercentBlack(double percentBlack)
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        if (percentBlack < 0 || percentBlack > 100)
            throw new ArgumentException("Percent black must be between 0 and 100");
        
        // Convert to grayscale and collect all intensity values
        int[] grayValues = new int[_width * _height];
        int idx = 0;
        
        for (int i = 0; i < _imageData.Length; i += 3)
        {
            grayValues[idx++] = (int)(0.299 * _imageData[i] + 0.587 * _imageData[i + 1] + 0.114 * _imageData[i + 2]);
        }
        
        // Sort grayscale values
        Array.Sort(grayValues);
        
        // Find threshold at the specified percentile
        int thresholdIndex = (int)(grayValues.Length * percentBlack / 100.0);
        thresholdIndex = Math.Clamp(thresholdIndex, 0, grayValues.Length - 1);
        int threshold = grayValues[thresholdIndex];
        
        // Apply binarization
        return BinarizeManual(threshold);
    }

    /// <summary>
    /// Applies Mean Iterative Selection (also known as Isodata) thresholding.
    /// Iteratively calculates the mean of pixels above and below threshold until convergence.
    /// </summary>
    public byte[] BinarizeMeanIterative()
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        
        // Convert to grayscale
        int[] grayValues = new int[_width * _height];
        int idx = 0;
        
        for (int i = 0; i < _imageData.Length; i += 3)
        {
            grayValues[idx++] = (int)(0.299 * _imageData[i] + 0.587 * _imageData[i + 1] + 0.114 * _imageData[i + 2]);
        }
        
        // Initial threshold is the mean of all pixels
        int threshold = grayValues.Sum() / grayValues.Length;
        int oldThreshold;
        
        const int maxIterations = 100;
        int iteration = 0;
        
        do
        {
            oldThreshold = threshold;
            
            // Calculate mean of pixels below and above threshold
            long sumBelow = 0, sumAbove = 0;
            int countBelow = 0, countAbove = 0;
            
            foreach (int gray in grayValues)
            {
                if (gray <= threshold)
                {
                    sumBelow += gray;
                    countBelow++;
                }
                else
                {
                    sumAbove += gray;
                    countAbove++;
                }
            }
            
            int meanBelow = countBelow > 0 ? (int)(sumBelow / countBelow) : 0;
            int meanAbove = countAbove > 0 ? (int)(sumAbove / countAbove) : 255;
            
            // New threshold is the average of the two means
            threshold = (meanBelow + meanAbove) / 2;
            
            iteration++;
        } while (threshold != oldThreshold && iteration < maxIterations);
        
        // Apply binarization
        return BinarizeManual(threshold);
    }

    /// <summary>
    /// Applies Entropy Selection (Kapur) thresholding.
    /// Maximizes the sum of entropies of foreground and background.
    /// </summary>
    public byte[] BinarizeEntropy()
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        
        // Calculate histogram
        int[] histogram = new int[256];
        for (int i = 0; i < _imageData.Length; i += 3)
        {
            int gray = (int)(0.299 * _imageData[i] + 0.587 * _imageData[i + 1] + 0.114 * _imageData[i + 2]);
            histogram[gray]++;
        }
        
        int totalPixels = _width * _height;
        
        // Calculate probability distribution
        double[] probability = new double[256];
        for (int i = 0; i < 256; i++)
        {
            probability[i] = (double)histogram[i] / totalPixels;
        }
        
        // Find threshold that maximizes entropy
        double maxEntropy = double.MinValue;
        int bestThreshold = 0;
        
        for (int t = 0; t < 256; t++)
        {
            // Calculate probability of background (0 to t)
            double probBackground = 0;
            for (int i = 0; i <= t; i++)
            {
                probBackground += probability[i];
            }
            
            // Calculate probability of foreground (t+1 to 255)
            double probForeground = 1.0 - probBackground;
            
            if (probBackground == 0 || probForeground == 0)
                continue;
            
            // Calculate entropy of background
            double entropyBackground = 0;
            for (int i = 0; i <= t; i++)
            {
                if (probability[i] > 0)
                {
                    double p = probability[i] / probBackground;
                    entropyBackground -= p * Math.Log(p);
                }
            }
            
            // Calculate entropy of foreground
            double entropyForeground = 0;
            for (int i = t + 1; i < 256; i++)
            {
                if (probability[i] > 0)
                {
                    double p = probability[i] / probForeground;
                    entropyForeground -= p * Math.Log(p);
                }
            }
            
            // Total entropy
            double totalEntropy = entropyBackground + entropyForeground;
            
            if (totalEntropy > maxEntropy)
            {
                maxEntropy = totalEntropy;
                bestThreshold = t;
            }
        }
        
        // Apply binarization
        return BinarizeManual(bestThreshold);
    }

    /// <summary>
    /// Applies Minimum Error (Kittler-Illingworth) thresholding.
    /// Assumes Gaussian distribution for foreground and background, minimizes classification error.
    /// </summary>
    public byte[] BinarizeMinimumError()
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        
        // Calculate histogram
        int[] histogram = new int[256];
        for (int i = 0; i < _imageData.Length; i += 3)
        {
            int gray = (int)(0.299 * _imageData[i] + 0.587 * _imageData[i + 1] + 0.114 * _imageData[i + 2]);
            histogram[gray]++;
        }
        
        int totalPixels = _width * _height;
        
        double minError = double.MaxValue;
        int bestThreshold = 0;
        
        for (int t = 1; t < 255; t++)
        {
            // Calculate statistics for background (0 to t)
            double P1 = 0, mean1 = 0;
            for (int i = 0; i <= t; i++)
            {
                P1 += histogram[i];
                mean1 += i * histogram[i];
            }
            
            if (P1 == 0) continue;
            mean1 /= P1;
            P1 /= totalPixels;
            
            double variance1 = 0;
            for (int i = 0; i <= t; i++)
            {
                double diff = i - mean1;
                variance1 += histogram[i] * diff * diff;
            }
            variance1 /= (P1 * totalPixels);
            
            // Calculate statistics for foreground (t+1 to 255)
            double P2 = 0, mean2 = 0;
            for (int i = t + 1; i < 256; i++)
            {
                P2 += histogram[i];
                mean2 += i * histogram[i];
            }
            
            if (P2 == 0) continue;
            mean2 /= P2;
            P2 /= totalPixels;
            
            double variance2 = 0;
            for (int i = t + 1; i < 256; i++)
            {
                double diff = i - mean2;
                variance2 += histogram[i] * diff * diff;
            }
            variance2 /= (P2 * totalPixels);
            
            // Avoid log of zero or negative values
            if (variance1 <= 0 || variance2 <= 0 || P1 <= 0 || P2 <= 0)
                continue;
            
            // Calculate criterion (Kittler-Illingworth)
            double J = 1.0 + 2.0 * (P1 * Math.Log(variance1) + P2 * Math.Log(variance2)) 
                       - 2.0 * (P1 * Math.Log(P1) + P2 * Math.Log(P2));
            
            if (J < minError)
            {
                minError = J;
                bestThreshold = t;
            }
        }
        
        // Apply binarization
        return BinarizeManual(bestThreshold);
    }

    /// <summary>
    /// Applies Fuzzy Minimum Error thresholding.
    /// Extension of minimum error method using fuzzy set theory.
    /// </summary>
    public byte[] BinarizeFuzzyMinimumError()
    {
        if (_imageData == null) throw new InvalidOperationException("No image loaded");
        
        // Calculate histogram
        int[] histogram = new int[256];
        for (int i = 0; i < _imageData.Length; i += 3)
        {
            int gray = (int)(0.299 * _imageData[i] + 0.587 * _imageData[i + 1] + 0.114 * _imageData[i + 2]);
            histogram[gray]++;
        }
        
        int totalPixels = _width * _height;
        
        double minError = double.MaxValue;
        int bestThreshold = 0;
        
        for (int t = 1; t < 255; t++)
        {
            // Calculate fuzzy membership for background and foreground
            double P1 = 0, P2 = 0;
            double mean1 = 0, mean2 = 0;
            
            // Fuzzy membership using S-function for background and Z-function for foreground
            for (int i = 0; i < 256; i++)
            {
                double membershipBg, membershipFg;
                
                if (i <= t)
                {
                    membershipBg = 1.0;
                    membershipFg = 0.0;
                }
                else
                {
                    membershipBg = 0.0;
                    membershipFg = 1.0;
                }
                
                // Fuzzy gradual transition near threshold
                int transitionWidth = 10;
                if (Math.Abs(i - t) <= transitionWidth)
                {
                    double dist = Math.Abs(i - t) / (double)transitionWidth;
                    if (i < t)
                    {
                        membershipBg = 1.0;
                        membershipFg = dist;
                    }
                    else
                    {
                        membershipBg = 1.0 - dist;
                        membershipFg = 1.0;
                    }
                }
                
                double weightedCount = histogram[i];
                P1 += membershipBg * weightedCount;
                P2 += membershipFg * weightedCount;
                mean1 += membershipBg * i * weightedCount;
                mean2 += membershipFg * i * weightedCount;
            }
            
            if (P1 == 0 || P2 == 0) continue;
            
            mean1 /= P1;
            mean2 /= P2;
            
            // Calculate fuzzy variances
            double variance1 = 0, variance2 = 0;
            for (int i = 0; i < 256; i++)
            {
                double membershipBg, membershipFg;
                
                if (i <= t)
                {
                    membershipBg = 1.0;
                    membershipFg = 0.0;
                }
                else
                {
                    membershipBg = 0.0;
                    membershipFg = 1.0;
                }
                
                int transitionWidth = 10;
                if (Math.Abs(i - t) <= transitionWidth)
                {
                    double dist = Math.Abs(i - t) / (double)transitionWidth;
                    if (i < t)
                    {
                        membershipBg = 1.0;
                        membershipFg = dist;
                    }
                    else
                    {
                        membershipBg = 1.0 - dist;
                        membershipFg = 1.0;
                    }
                }
                
                variance1 += membershipBg * histogram[i] * Math.Pow(i - mean1, 2);
                variance2 += membershipFg * histogram[i] * Math.Pow(i - mean2, 2);
            }
            
            variance1 /= P1;
            variance2 /= P2;
            
            if (variance1 <= 0 || variance2 <= 0) continue;
            
            P1 /= totalPixels;
            P2 /= totalPixels;
            
            if (P1 <= 0 || P2 <= 0) continue;
            
            // Calculate fuzzy minimum error criterion
            double J = 1.0 + 2.0 * (P1 * Math.Log(variance1) + P2 * Math.Log(variance2))
                       - 2.0 * (P1 * Math.Log(P1) + P2 * Math.Log(P2));
            
            if (J < minError)
            {
                minError = J;
                bestThreshold = t;
            }
        }
        
        // Apply binarization
        return BinarizeManual(bestThreshold);
    }
}

