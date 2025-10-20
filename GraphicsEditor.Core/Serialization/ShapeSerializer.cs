using System.Text.Json;
using GraphicsEditor.Core.Models;

namespace GraphicsEditor.Core.Serialization;

/// <summary>
/// Provides serialization and deserialization for shape collections.
/// </summary>
public static class ShapeSerializer
{
    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    /// <summary>
    /// Serializes a collection of shapes to JSON string.
    /// </summary>
    public static string SerializeShapes(IEnumerable<IShape> shapes)
    {
        return JsonSerializer.Serialize(shapes, _options);
    }

    /// <summary>
    /// Serializes shapes to a file.
    /// </summary>
    public static async Task SerializeToFileAsync(IEnumerable<IShape> shapes, string filePath)
    {
        string json = SerializeShapes(shapes);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Deserializes shapes from JSON string.
    /// </summary>
    public static IEnumerable<IShape> DeserializeShapes(string json)
    {
        var shapes = JsonSerializer.Deserialize<List<IShape>>(json, _options);
        return shapes ?? new List<IShape>();
    }

    /// <summary>
    /// Deserializes shapes from a file.
    /// </summary>
    public static async Task<IEnumerable<IShape>> DeserializeFromFileAsync(string filePath)
    {
        string json = await File.ReadAllTextAsync(filePath);
        return DeserializeShapes(json);
    }

    /// <summary>
    /// Exports shapes to a specific format (JSON or XML).
    /// </summary>
    public static async Task ExportAsync(IEnumerable<IShape> shapes, string filePath, ExportFormat format = ExportFormat.Json)
    {
        switch (format)
        {
            case ExportFormat.Json:
                await SerializeToFileAsync(shapes, filePath);
                break;
            default:
                throw new NotSupportedException($"Export format {format} is not supported.");
        }
    }

    /// <summary>
    /// Imports shapes from a specific format.
    /// </summary>
    public static async Task<IEnumerable<IShape>> ImportAsync(string filePath)
    {
        return await DeserializeFromFileAsync(filePath);
    }
}

public enum ExportFormat
{
    Json,
    Xml
}
