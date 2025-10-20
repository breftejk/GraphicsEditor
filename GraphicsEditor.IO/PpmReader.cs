using System.Text;
using System.Linq;

namespace GraphicsEditor.IO;

/// <summary>
/// Reads PPM image files (both P3 ASCII and P6 binary formats).
/// Uses block-based reading for efficient handling of large P6 files.
/// </summary>
public class PpmReader
{
    /// <summary>
    /// Reads a PPM file and returns image data.
    /// </summary>
    public PpmImage Read(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        
        // Read magic number (P3 or P6)
        string? magicNumber = ReadLine(stream)?.Trim();
        if (magicNumber != "P3" && magicNumber != "P6")
        {
            throw new InvalidDataException($"Invalid PPM format. Expected P3 or P6, got: {magicNumber}");
        }

        bool isBinary = magicNumber == "P6";

        // Skip comments and empty lines, then read dimensions
        // Dimensions can be on one line "width height" or on separate lines
        var dimensionValues = new System.Collections.Generic.List<int>();
        var extraValuesAfterDimensions = new System.Collections.Generic.List<string>();
        
        while (dimensionValues.Count < 2)
        {
            string? line = ReadLine(stream);
            if (line == null)
                throw new InvalidDataException("Invalid PPM file: missing dimensions");
            
            // Remove inline comments
            int commentIndex = line.IndexOf('#');
            if (commentIndex >= 0)
            {
                line = line.Substring(0, commentIndex);
            }
            
            line = line.Trim();
            
            if (line.Length > 0)
            {
                // Split by any whitespace (space, tab, etc.)
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (dimensionValues.Count < 2 && int.TryParse(part, out int val))
                    {
                        dimensionValues.Add(val);
                    }
                    else if (dimensionValues.Count >= 2)
                    {
                        // Save extra values that came after the dimensions
                        extraValuesAfterDimensions.Add(part);
                    }
                }
            }
        }

        if (dimensionValues.Count < 2)
            throw new InvalidDataException("Invalid PPM file: malformed dimensions");

        int width = dimensionValues[0];
        int height = dimensionValues[1];

        // Skip comments and empty lines, then read max color value
        int maxColor = 0;
        string? extraPixelData = null; // Store any pixel values on same line as maxColor
        
        // First, check if maxColor was already in the extra values from dimensions line
        if (extraValuesAfterDimensions.Count > 0 && int.TryParse(extraValuesAfterDimensions[0], out int firstExtraValue))
        {
            maxColor = firstExtraValue;
            // Any remaining extra values after maxColor are pixel data
            if (extraValuesAfterDimensions.Count > 1)
            {
                extraPixelData = string.Join(" ", extraValuesAfterDimensions.Skip(1));
            }
        }
        else
        {
            // Read maxColor from a new line
            while (maxColor == 0)
            {
                string? line = ReadLine(stream);
                if (line == null)
                    throw new InvalidDataException("Invalid PPM file: missing max color value");
                
                // Remove inline comments
                int commentIndex = line.IndexOf('#');
                if (commentIndex >= 0)
                {
                    line = line.Substring(0, commentIndex);
                }
                
                line = line.Trim();
                
                if (line.Length > 0)
                {
                    // Take only the first number from the line
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0 && int.TryParse(parts[0], out int value))
                    {
                        maxColor = value;
                        
                        // If there are more values on this line after maxColor (for P3),
                        // save them to be prepended to pixel data
                        if (parts.Length > 1 && !isBinary)
                        {
                            extraPixelData = string.Join(" ", parts.Skip(1));
                        }
                    }
                }
            }
        }

        if (maxColor < 1 || maxColor > 65535)
            throw new InvalidDataException($"Invalid max color value: {maxColor}. Must be between 1 and 65535");

        // Read pixel data
        byte[] pixelData;
        if (isBinary)
        {
            pixelData = ReadP6PixelData(stream, width, height, maxColor);
        }
        else
        {
            // For P3, read remaining text
            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            pixelData = ReadP3PixelData(reader, width, height, maxColor, extraPixelData);
        }

        return new PpmImage
        {
            Width = width,
            Height = height,
            MaxColorValue = maxColor,
            Format = isBinary ? PpmFormat.P6 : PpmFormat.P3,
            PixelData = pixelData
        };
    }

    /// <summary>
    /// Reads P3 (ASCII) pixel data.
    /// </summary>
    private byte[] ReadP3PixelData(StreamReader reader, int width, int height, int maxColor, string? extraPixelData = null)
    {
        int totalPixels = width * height * 3;
        byte[] pixelData = new byte[totalPixels];

        string? remainingText = reader.ReadToEnd();
        if (remainingText == null)
            throw new InvalidDataException("Invalid P3 file: no pixel data");

        // Prepend any extra pixel data from maxColor line
        if (!string.IsNullOrEmpty(extraPixelData))
        {
            remainingText = extraPixelData + " " + remainingText;
        }

        // Remove comments (both full-line and inline comments)
        var lines = remainingText.Split('\n');
        var nonCommentLines = new System.Collections.Generic.List<string>();
        foreach (var line in lines)
        {
            var processedLine = line;
            
            // Remove inline comments (everything after #)
            int commentIndex = line.IndexOf('#');
            if (commentIndex >= 0)
            {
                processedLine = line.Substring(0, commentIndex);
            }
            
            var trimmed = processedLine.Trim();
            if (trimmed.Length > 0)
            {
                nonCommentLines.Add(processedLine);
            }
        }
        remainingText = string.Join(" ", nonCommentLines);

        // Parse values more carefully to handle edge cases
        var valuesList = new System.Collections.Generic.List<int>();
        var tokens = remainingText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var token in tokens)
        {
            if (int.TryParse(token, out int value))
            {
                valuesList.Add(value);
            }
            // Silently skip non-numeric tokens (could be remnants of comments)
        }

        if (valuesList.Count < totalPixels)
        {
            // Debug: Check if we're close (might help diagnose the issue)
            int missing = totalPixels - valuesList.Count;
            throw new InvalidDataException($"Invalid P3 file: expected {totalPixels} values, got {valuesList.Count} (missing {missing} values)");
        }

        for (int i = 0; i < totalPixels; i++)
        {
            int value = valuesList[i];
            
            if (value < 0 || value > maxColor)
                throw new InvalidDataException($"Invalid P3 file: pixel value {value} out of range [0, {maxColor}] at index {i}");
            
            // Scale to 8-bit (0-255)
            pixelData[i] = (byte)((value * 255) / maxColor);
        }

        return pixelData;
    }

    /// <summary>
    /// Reads P6 (binary) pixel data using block-based reading for performance.
    /// </summary>
    private byte[] ReadP6PixelData(Stream stream, int width, int height, int maxColor)
    {
        bool is16Bit = maxColor > 255;
        int totalPixels = width * height * 3;
        byte[] pixelData = new byte[totalPixels];

        if (is16Bit)
        {
            // Read 16-bit data and convert to 8-bit
            int totalBytes16 = totalPixels * 2; // 2 bytes per value
            byte[] buffer16 = new byte[totalBytes16];
            
            int blockSize = 8192; // 8KB blocks
            int bytesRead = 0;

            while (bytesRead < totalBytes16)
            {
                int toRead = Math.Min(blockSize, totalBytes16 - bytesRead);
                int read = stream.Read(buffer16, bytesRead, toRead);

                if (read == 0)
                    throw new InvalidDataException("Unexpected end of file while reading P6 16-bit pixel data");

                bytesRead += read;
            }

            // Convert 16-bit big-endian to 8-bit
            for (int i = 0; i < totalPixels; i++)
            {
                ushort value16 = (ushort)((buffer16[i * 2] << 8) | buffer16[i * 2 + 1]);
                // Scale to 8-bit
                pixelData[i] = (byte)((value16 * 255) / maxColor);
            }
        }
        else
        {
            // Read 8-bit data
            int blockSize = 4096; // 4KB blocks
            int bytesRead = 0;

            while (bytesRead < totalPixels)
            {
                int toRead = Math.Min(blockSize, totalPixels - bytesRead);
                int read = stream.Read(pixelData, bytesRead, toRead);

                if (read == 0)
                    throw new InvalidDataException("Unexpected end of file while reading P6 pixel data");

                bytesRead += read;
            }
            
            // Scale if maxColor is not 255
            if (maxColor != 255)
            {
                for (int i = 0; i < totalPixels; i++)
                {
                    pixelData[i] = (byte)((pixelData[i] * 255) / maxColor);
                }
            }
        }

        return pixelData;
    }

    /// <summary>
    /// Reads a single line from the stream (helper for header parsing).
    /// </summary>
    private string? ReadLine(Stream stream)
    {
        var sb = new StringBuilder();
        int b;
        
        while ((b = stream.ReadByte()) != -1)
        {
            if (b == '\n')
                break;
            if (b != '\r') // Skip CR
                sb.Append((char)b);
        }
        
        return b == -1 && sb.Length == 0 ? null : sb.ToString();
    }
}

/// <summary>
/// Represents a PPM image.
/// </summary>
public class PpmImage
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int MaxColorValue { get; set; }
    public PpmFormat Format { get; set; }
    public byte[] PixelData { get; set; } = Array.Empty<byte>();
}

public enum PpmFormat
{
    P3, // ASCII
    P6  // Binary
}
