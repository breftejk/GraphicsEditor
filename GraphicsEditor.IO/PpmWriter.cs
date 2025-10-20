using System.Text;

namespace GraphicsEditor.IO;

/// <summary>
/// Writes PPM image files in P3 (ASCII) or P6 (binary) format.
/// </summary>
public class PpmWriter
{
    /// <summary>
    /// Writes a PPM image to a file.
    /// </summary>
    public void Write(string filePath, PpmImage image)
    {
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);

        if (image.Format == PpmFormat.P6)
        {
            WriteP6(stream, image);
        }
        else
        {
            WriteP3(stream, image);
        }
    }

    /// <summary>
    /// Writes PPM in P3 (ASCII) format.
    /// </summary>
    private void WriteP3(Stream stream, PpmImage image)
    {
        using var writer = new StreamWriter(stream, Encoding.ASCII);

        // Write header
        writer.WriteLine("P3");
        writer.WriteLine($"{image.Width} {image.Height}");
        writer.WriteLine(image.MaxColorValue);

        // Write pixel data (15 values per line for readability)
        int valuesPerLine = 15;
        int count = 0;

        for (int i = 0; i < image.PixelData.Length; i++)
        {
            writer.Write(image.PixelData[i]);

            count++;
            if (count >= valuesPerLine || i == image.PixelData.Length - 1)
            {
                writer.WriteLine();
                count = 0;
            }
            else
            {
                writer.Write(' ');
            }
        }
    }

    /// <summary>
    /// Writes PPM in P6 (binary) format.
    /// </summary>
    private void WriteP6(Stream stream, PpmImage image)
    {
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true);

        // Write header
        writer.WriteLine("P6");
        writer.WriteLine($"{image.Width} {image.Height}");
        writer.WriteLine(image.MaxColorValue);
        writer.Flush();

        // Write binary pixel data
        stream.Write(image.PixelData, 0, image.PixelData.Length);
    }

    /// <summary>
    /// Creates a PPM image from raw RGB data.
    /// </summary>
    public static PpmImage CreateFromRgbData(byte[] rgbData, int width, int height, PpmFormat format = PpmFormat.P6)
    {
        return new PpmImage
        {
            Width = width,
            Height = height,
            MaxColorValue = 255,
            Format = format,
            PixelData = rgbData
        };
    }
}
