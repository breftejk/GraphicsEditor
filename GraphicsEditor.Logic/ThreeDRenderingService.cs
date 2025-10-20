using GraphicsEditor.Core.Models;
using GraphicsEditor.Core.Conversions;
using GraphicsEditor.Core.Geometry;

namespace GraphicsEditor.Logic;

/// <summary>
/// Provides 3D rendering data for RGB Cube visualization.
/// </summary>
public class ThreeDRenderingService
{
    /// <summary>
    /// Generates vertices for an RGB cube.
    /// The cube corners represent: (0,0,0)=Black, (1,1,1)=White, etc.
    /// </summary>
    public List<RgbCubeVertex> GenerateRgbCubeVertices(int resolution = 10)
    {
        var vertices = new List<RgbCubeVertex>();

        // Generate cube faces with color gradients
        for (int i = 0; i <= resolution; i++)
        {
            for (int j = 0; j <= resolution; j++)
            {
                double u = i / (double)resolution;
                double v = j / (double)resolution;

                // Add vertices for all 6 faces of the RGB cube
                // Face 1: R=0 (YZ plane, Cyan-Blue-Green-Black)
                vertices.Add(new RgbCubeVertex(0, u, v, new RgbColor(0, (byte)(u * 255), (byte)(v * 255))));

                // Face 2: R=1 (YZ plane, Yellow-White-Magenta-Red)
                vertices.Add(new RgbCubeVertex(1, u, v, new RgbColor(255, (byte)(u * 255), (byte)(v * 255))));

                // Face 3: G=0 (XZ plane, Blue-Magenta-Black-Red)
                vertices.Add(new RgbCubeVertex(u, 0, v, new RgbColor((byte)(u * 255), 0, (byte)(v * 255))));

                // Face 4: G=1 (XZ plane, Cyan-White-Yellow-Green)
                vertices.Add(new RgbCubeVertex(u, 1, v, new RgbColor((byte)(u * 255), 255, (byte)(v * 255))));

                // Face 5: B=0 (XY plane, Black-Red-Green-Yellow)
                vertices.Add(new RgbCubeVertex(u, v, 0, new RgbColor((byte)(u * 255), (byte)(v * 255), 0)));

                // Face 6: B=1 (XY plane, Blue-Magenta-Cyan-White)
                vertices.Add(new RgbCubeVertex(u, v, 1, new RgbColor((byte)(u * 255), (byte)(v * 255), 255)));
            }
        }

        return vertices;
    }

    /// <summary>
    /// Generates a cross-section of the RGB cube at a specific position.
    /// </summary>
    public List<RgbCubeVertex> GenerateRgbCubeCrossSection(CrossSectionAxis axis, double position, int resolution = 50)
    {
        var vertices = new List<RgbCubeVertex>();
        position = Math.Clamp(position, 0, 1);

        for (int i = 0; i <= resolution; i++)
        {
            for (int j = 0; j <= resolution; j++)
            {
                double u = i / (double)resolution;
                double v = j / (double)resolution;

                RgbColor color;
                Point2D point;

                switch (axis)
                {
                    case CrossSectionAxis.X: // Slice perpendicular to R axis
                        color = new RgbColor((byte)(position * 255), (byte)(u * 255), (byte)(v * 255));
                        point = new Point2D(u, v);
                        break;

                    case CrossSectionAxis.Y: // Slice perpendicular to G axis
                        color = new RgbColor((byte)(u * 255), (byte)(position * 255), (byte)(v * 255));
                        point = new Point2D(u, v);
                        break;

                    case CrossSectionAxis.Z: // Slice perpendicular to B axis
                        color = new RgbColor((byte)(u * 255), (byte)(v * 255), (byte)(position * 255));
                        point = new Point2D(u, v);
                        break;

                    default:
                        throw new ArgumentException("Invalid axis");
                }

                vertices.Add(new RgbCubeVertex(point.X, point.Y, position, color));
            }
        }

        return vertices;
    }

    /// <summary>
    /// Generates vertices for an HSV cone visualization.
    /// </summary>
    public List<HsvConeVertex> GenerateHsvConeVertices(int radialSegments = 36, int heightSegments = 10)
    {
        var vertices = new List<HsvConeVertex>();

        for (int h = 0; h <= heightSegments; h++)
        {
            double value = h / (double)heightSegments; // 0 at tip, 1 at base
            double radius = value; // Cone radius increases with height

            for (int r = 0; r <= radialSegments; r++)
            {
                double hue = (r / (double)radialSegments) * 360;
                double saturation = 100;

                // Convert HSV to RGB for color
                var hsvColor = new HsvColor(hue, saturation, value * 100);
                var rgbColor = HsvConverter.HsvToRgb(hsvColor);

                // 3D position (cylindrical coordinates)
                double angle = hue * Math.PI / 180.0;
                double x = radius * Math.Cos(angle);
                double y = value; // Height
                double z = radius * Math.Sin(angle);

                vertices.Add(new HsvConeVertex(x, y, z, hsvColor, rgbColor));
            }
        }

        return vertices;
    }

    /// <summary>
    /// Applies rotation transformation to 3D vertices.
    /// </summary>
    public void ApplyRotation(List<RgbCubeVertex> vertices, double angleX, double angleY, double angleZ)
    {
        double radX = angleX * Math.PI / 180.0;
        double radY = angleY * Math.PI / 180.0;
        double radZ = angleZ * Math.PI / 180.0;

        foreach (var vertex in vertices)
        {
            // Translate to origin (center of cube is at 0.5, 0.5, 0.5)
            double x = vertex.X - 0.5;
            double y = vertex.Y - 0.5;
            double z = vertex.Z - 0.5;

            // Rotate around X axis
            double y1 = y * Math.Cos(radX) - z * Math.Sin(radX);
            double z1 = y * Math.Sin(radX) + z * Math.Cos(radX);

            // Rotate around Y axis
            double x2 = x * Math.Cos(radY) + z1 * Math.Sin(radY);
            double z2 = -x * Math.Sin(radY) + z1 * Math.Cos(radY);

            // Rotate around Z axis
            double x3 = x2 * Math.Cos(radZ) - y1 * Math.Sin(radZ);
            double y3 = x2 * Math.Sin(radZ) + y1 * Math.Cos(radZ);

            // Translate back
            vertex.X = x3 + 0.5;
            vertex.Y = y3 + 0.5;
            vertex.Z = z2 + 0.5;
        }
    }
}

/// <summary>
/// Represents a vertex in the RGB cube with position and color.
/// </summary>
public class RgbCubeVertex
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public RgbColor Color { get; set; }

    public RgbCubeVertex(double x, double y, double z, RgbColor color)
    {
        X = x;
        Y = y;
        Z = z;
        Color = color;
    }
}

/// <summary>
/// Represents a vertex in the HSV cone.
/// </summary>
public class HsvConeVertex
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public HsvColor HsvColor { get; set; }
    public RgbColor RgbColor { get; set; }

    public HsvConeVertex(double x, double y, double z, HsvColor hsvColor, RgbColor rgbColor)
    {
        X = x;
        Y = y;
        Z = z;
        HsvColor = hsvColor;
        RgbColor = rgbColor;
    }
}

public enum CrossSectionAxis
{
    X, // Red axis
    Y, // Green axis
    Z  // Blue axis
}
