using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace GraphicsEditor.IO;

/// <summary>
/// Reads JPEG image files using ImageSharp library.
/// </summary>
public class JpegReader
{
    /// <summary>
    /// Reads a JPEG file and returns image data as RGB bytes.
    /// </summary>
    public JpegImage Read(string filePath)
    {
        try
        {
            using var image = Image.Load<Rgb24>(filePath);
            
            int width = image.Width;
            int height = image.Height;
            byte[] pixelData = new byte[width * height * 3];

            // Extract RGB pixel data
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

            return new JpegImage
            {
                Width = width,
                Height = height,
                PixelData = pixelData
            };
        }
        catch (UnknownImageFormatException ex)
        {
            throw new InvalidDataException("The file is not a valid JPEG image.", ex);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to read JPEG file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Reads image metadata without loading full pixel data.
    /// </summary>
    public (int width, int height) ReadMetadata(string filePath)
    {
        try
        {
            using var image = Image.Load(filePath);
            return (image.Width, image.Height);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to read image metadata: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Represents a JPEG image.
/// </summary>
public class JpegImage
{
    public int Width { get; set; }
    public int Height { get; set; }
    public byte[] PixelData { get; set; } = Array.Empty<byte>();
}
