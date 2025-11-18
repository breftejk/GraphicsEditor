using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace GraphicsEditor.IO;

/// <summary>
/// Writes PNG image files with support for RGB and RGBA formats.
/// </summary>
public class PngWriter
{
    /// <summary>
    /// Writes RGBA pixel data to a PNG file.
    /// </summary>
    /// <param name="filePath">Output file path</param>
    /// <param name="pixelData">RGBA pixel data (4 bytes per pixel)</param>
    /// <param name="width">Image width</param>
    /// <param name="height">Image height</param>
    /// <param name="compressionLevel">PNG compression level (0-9, higher is more compressed)</param>
    public void Write(string filePath, byte[] pixelData, int width, int height, PngCompressionLevel compressionLevel = PngCompressionLevel.DefaultCompression)
    {
        if (pixelData.Length != width * height * 4)
            throw new ArgumentException("Pixel data length does not match image dimensions (expected RGBA)");

        try
        {
            using var image = Image.LoadPixelData<Rgba32>(pixelData, width, height);
            
            var encoder = new PngEncoder
            {
                CompressionLevel = compressionLevel
            };

            image.Save(filePath, encoder);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to write PNG file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Writes RGB pixel data to a PNG file (without alpha channel).
    /// </summary>
    /// <param name="filePath">Output file path</param>
    /// <param name="pixelData">RGB pixel data (3 bytes per pixel)</param>
    /// <param name="width">Image width</param>
    /// <param name="height">Image height</param>
    /// <param name="compressionLevel">PNG compression level (0-9, higher is more compressed)</param>
    public void WriteRgb(string filePath, byte[] pixelData, int width, int height, PngCompressionLevel compressionLevel = PngCompressionLevel.DefaultCompression)
    {
        if (pixelData.Length != width * height * 3)
            throw new ArgumentException("Pixel data length does not match image dimensions (expected RGB)");

        try
        {
            using var image = Image.LoadPixelData<Rgb24>(pixelData, width, height);
            
            var encoder = new PngEncoder
            {
                CompressionLevel = compressionLevel
            };

            image.Save(filePath, encoder);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to write PNG file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Writes a PngImage to a file.
    /// </summary>
    public void Write(string filePath, PngImage image, PngCompressionLevel compressionLevel = PngCompressionLevel.DefaultCompression)
    {
        if (image.HasAlpha)
        {
            Write(filePath, image.PixelData, image.Width, image.Height, compressionLevel);
        }
        else
        {
            WriteRgb(filePath, image.PixelData, image.Width, image.Height, compressionLevel);
        }
    }

    /// <summary>
    /// Converts a PPM image to PNG format and saves it.
    /// </summary>
    public void ConvertPpmToPng(string ppmFilePath, string pngFilePath, PngCompressionLevel compressionLevel = PngCompressionLevel.DefaultCompression)
    {
        var ppmReader = new PpmReader();
        var ppmImage = ppmReader.Read(ppmFilePath);

        WriteRgb(pngFilePath, ppmImage.PixelData, ppmImage.Width, ppmImage.Height, compressionLevel);
    }

    /// <summary>
    /// Converts a JPEG image to PNG format and saves it.
    /// </summary>
    public void ConvertJpegToPng(string jpegFilePath, string pngFilePath, PngCompressionLevel compressionLevel = PngCompressionLevel.DefaultCompression)
    {
        var jpegReader = new JpegReader();
        var jpegImage = jpegReader.Read(jpegFilePath);

        WriteRgb(pngFilePath, jpegImage.PixelData, jpegImage.Width, jpegImage.Height, compressionLevel);
    }
}

