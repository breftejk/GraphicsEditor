using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace GraphicsEditor.IO;

/// <summary>
/// Reads PNG image files using ImageSharp library.
/// Supports both RGB and RGBA formats.
/// </summary>
public class PngReader
{
    /// <summary>
    /// Reads a PNG file and returns image data as RGBA bytes.
    /// </summary>
    public PngImage Read(string filePath)
    {
        try
        {
            using var image = Image.Load<Rgba32>(filePath);
            
            int width = image.Width;
            int height = image.Height;
            byte[] pixelData = new byte[width * height * 4];

            // Extract RGBA pixel data
            image.ProcessPixelRows(accessor =>
            {
                int index = 0;
                for (int y = 0; y < height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < width; x++)
                    {
                        var pixel = row[x];
                        pixelData[index++] = pixel.R;
                        pixelData[index++] = pixel.G;
                        pixelData[index++] = pixel.B;
                        pixelData[index++] = pixel.A;
                    }
                }
            });

            return new PngImage
            {
                Width = width,
                Height = height,
                PixelData = pixelData
            };
        }
        catch (UnknownImageFormatException ex)
        {
            throw new InvalidDataException("The file is not a valid PNG image.", ex);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to read PNG file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Reads a PNG file and returns image data as RGB bytes (discarding alpha).
    /// </summary>
    public PngImage ReadAsRgb(string filePath)
    {
        try
        {
            using var image = Image.Load<Rgba32>(filePath);
            
            int width = image.Width;
            int height = image.Height;
            byte[] pixelData = new byte[width * height * 3];

            // Extract RGB pixel data (discard alpha)
            image.ProcessPixelRows(accessor =>
            {
                int index = 0;
                for (int y = 0; y < height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < width; x++)
                    {
                        var pixel = row[x];
                        pixelData[index++] = pixel.R;
                        pixelData[index++] = pixel.G;
                        pixelData[index++] = pixel.B;
                    }
                }
            });

            return new PngImage
            {
                Width = width,
                Height = height,
                PixelData = pixelData,
                HasAlpha = false
            };
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to read PNG file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Reads image metadata without loading full pixel data.
    /// </summary>
    public (int width, int height, bool hasAlpha) ReadMetadata(string filePath)
    {
        try
        {
            using var image = Image.Load(filePath);
            var hasAlpha = image.PixelType.AlphaRepresentation != PixelAlphaRepresentation.None;
            return (image.Width, image.Height, hasAlpha);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to read image metadata: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Represents a PNG image.
/// </summary>
public class PngImage
{
    public int Width { get; set; }
    public int Height { get; set; }
    public byte[] PixelData { get; set; } = Array.Empty<byte>();
    public bool HasAlpha { get; set; } = true;
}

