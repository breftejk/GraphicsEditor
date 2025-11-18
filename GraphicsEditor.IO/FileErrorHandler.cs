namespace GraphicsEditor.IO;

/// <summary>
/// Handles file operation errors and provides user-friendly error messages.
/// </summary>
public static class FileErrorHandler
{
    /// <summary>
    /// Validates if a file exists and is readable.
    /// </summary>
    public static void ValidateFileExists(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");
    }

    /// <summary>
    /// Validates if a file path is writable.
    /// </summary>
    public static void ValidateFileWritable(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    /// Gets a user-friendly error message for common file errors.
    /// </summary>
    public static string GetUserFriendlyMessage(Exception ex)
    {
        return ex switch
        {
            FileNotFoundException => "The specified file was not found. Please check the file path and try again.",
            UnauthorizedAccessException => "Access denied. You may not have permission to access this file.",
            InvalidDataException => $"Invalid file format: {ex.Message}",
            NotSupportedException => $"Unsupported format: {ex.Message}",
            IOException => $"An I/O error occurred: {ex.Message}",
            _ => $"An unexpected error occurred: {ex.Message}"
        };
    }

    /// <summary>
    /// Determines if a file is a supported image format based on extension.
    /// </summary>
    public static bool IsSupportedImageFormat(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".ppm" => true,
            ".jpg" => true,
            ".jpeg" => true,
            ".png" => true,
            _ => false
        };
    }

    /// <summary>
    /// Detects image format from file extension.
    /// </summary>
    public static ImageFormat DetectImageFormat(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".ppm" => ImageFormat.Ppm,
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".png" => ImageFormat.Png,
            _ => throw new NotSupportedException($"Unsupported image format: {extension}")
        };
    }
}

public enum ImageFormat
{
    Ppm,
    Jpeg,
    Png
}
