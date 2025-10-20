using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace GraphicsEditor.IO;

/// <summary>
/// Writes JPEG image files with adjustable compression quality.
/// </summary>
public class JpegWriter
{
    /// <summary>
    /// Writes RGB pixel data to a JPEG file with specified quality.
    /// </summary>
    /// <param name="filePath">Output file path</param>
    /// <param name="pixelData">RGB pixel data (3 bytes per pixel)</param>
    /// <param name="width">Image width</param>
    /// <param name="height">Image height</param>
    /// <param name="quality">JPEG quality (1-100, higher is better)</param>
    public void Write(string filePath, byte[] pixelData, int width, int height, int quality = 90)
    {
        if (quality < 1 || quality > 100)
            throw new ArgumentException("Quality must be between 1 and 100", nameof(quality));

        if (pixelData.Length != width * height * 3)
            throw new ArgumentException("Pixel data length does not match image dimensions");

        try
        {
            using var image = Image.LoadPixelData<Rgb24>(pixelData, width, height);
            
            var encoder = new JpegEncoder
            {
                Quality = quality
            };

            image.Save(filePath, encoder);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to write JPEG file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Writes a JpegImage to a file.
    /// </summary>
    public void Write(string filePath, JpegImage image, int quality = 90)
    {
        Write(filePath, image.PixelData, image.Width, image.Height, quality);
    }

    /// <summary>
    /// Converts a PPM image to JPEG format and saves it.
    /// </summary>
    public void ConvertPpmToJpeg(string ppmFilePath, string jpegFilePath, int quality = 90)
    {
        var ppmReader = new PpmReader();
        var ppmImage = ppmReader.Read(ppmFilePath);

        Write(jpegFilePath, ppmImage.PixelData, ppmImage.Width, ppmImage.Height, quality);
    }
}
