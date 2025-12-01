using System.Text.Json.Serialization;

namespace GraphicsEditor.Core.Geometry;

/// <summary>
/// Represents a 3x3 matrix for 2D homogeneous coordinate transformations.
/// Supports translation, rotation, and scaling operations.
/// </summary>
public struct Matrix3x3 : IEquatable<Matrix3x3>
{
    // Matrix elements stored in row-major order:
    // | M11 M12 M13 |
    // | M21 M22 M23 |
    // | M31 M32 M33 |
    public double M11 { get; set; }
    public double M12 { get; set; }
    public double M13 { get; set; }
    public double M21 { get; set; }
    public double M22 { get; set; }
    public double M23 { get; set; }
    public double M31 { get; set; }
    public double M32 { get; set; }
    public double M33 { get; set; }

    /// <summary>
    /// Creates a matrix with the specified elements.
    /// </summary>
    public Matrix3x3(double m11, double m12, double m13,
                     double m21, double m22, double m23,
                     double m31, double m32, double m33)
    {
        M11 = m11; M12 = m12; M13 = m13;
        M21 = m21; M22 = m22; M23 = m23;
        M31 = m31; M32 = m32; M33 = m33;
    }

    /// <summary>
    /// Gets the identity matrix.
    /// </summary>
    [JsonIgnore]
    public static Matrix3x3 Identity => new Matrix3x3(
        1, 0, 0,
        0, 1, 0,
        0, 0, 1);

    /// <summary>
    /// Creates a translation matrix.
    /// </summary>
    public static Matrix3x3 CreateTranslation(double tx, double ty)
    {
        return new Matrix3x3(
            1, 0, tx,
            0, 1, ty,
            0, 0, 1);
    }

    /// <summary>
    /// Creates a rotation matrix around the origin.
    /// </summary>
    /// <param name="angleRadians">Rotation angle in radians</param>
    public static Matrix3x3 CreateRotation(double angleRadians)
    {
        double cos = Math.Cos(angleRadians);
        double sin = Math.Sin(angleRadians);
        return new Matrix3x3(
            cos, -sin, 0,
            sin, cos, 0,
            0, 0, 1);
    }

    /// <summary>
    /// Creates a rotation matrix around a specified pivot point.
    /// </summary>
    /// <param name="pivot">The point to rotate around</param>
    /// <param name="angleRadians">Rotation angle in radians</param>
    public static Matrix3x3 CreateRotationAround(Point2D pivot, double angleRadians)
    {
        // Translate to origin, rotate, translate back
        var toOrigin = CreateTranslation(-pivot.X, -pivot.Y);
        var rotation = CreateRotation(angleRadians);
        var fromOrigin = CreateTranslation(pivot.X, pivot.Y);
        return fromOrigin * rotation * toOrigin;
    }

    /// <summary>
    /// Creates a uniform scaling matrix from the origin.
    /// </summary>
    public static Matrix3x3 CreateScale(double scale)
    {
        return CreateScale(scale, scale);
    }

    /// <summary>
    /// Creates a non-uniform scaling matrix from the origin.
    /// </summary>
    public static Matrix3x3 CreateScale(double sx, double sy)
    {
        return new Matrix3x3(
            sx, 0, 0,
            0, sy, 0,
            0, 0, 1);
    }

    /// <summary>
    /// Creates a scaling matrix around a specified pivot point.
    /// </summary>
    /// <param name="pivot">The point to scale around</param>
    /// <param name="scale">Uniform scale factor</param>
    public static Matrix3x3 CreateScaleAround(Point2D pivot, double scale)
    {
        return CreateScaleAround(pivot, scale, scale);
    }

    /// <summary>
    /// Creates a non-uniform scaling matrix around a specified pivot point.
    /// </summary>
    /// <param name="pivot">The point to scale around</param>
    /// <param name="sx">Scale factor in X direction</param>
    /// <param name="sy">Scale factor in Y direction</param>
    public static Matrix3x3 CreateScaleAround(Point2D pivot, double sx, double sy)
    {
        // Translate to origin, scale, translate back
        var toOrigin = CreateTranslation(-pivot.X, -pivot.Y);
        var scaling = CreateScale(sx, sy);
        var fromOrigin = CreateTranslation(pivot.X, pivot.Y);
        return fromOrigin * scaling * toOrigin;
    }

    /// <summary>
    /// Multiplies two matrices.
    /// </summary>
    public static Matrix3x3 operator *(Matrix3x3 a, Matrix3x3 b)
    {
        return new Matrix3x3(
            a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31,
            a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32,
            a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33,

            a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31,
            a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32,
            a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33,

            a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31,
            a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32,
            a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33);
    }

    /// <summary>
    /// Transforms a point using this matrix.
    /// </summary>
    public Point2D Transform(Point2D point)
    {
        // Homogeneous coordinate: [x, y, 1]
        double x = M11 * point.X + M12 * point.Y + M13;
        double y = M21 * point.X + M22 * point.Y + M23;
        double w = M31 * point.X + M32 * point.Y + M33;

        // Normalize (divide by w) for perspective transformations
        if (Math.Abs(w) > 1e-10 && Math.Abs(w - 1.0) > 1e-10)
        {
            x /= w;
            y /= w;
        }

        return new Point2D(x, y);
    }

    public bool Equals(Matrix3x3 other)
    {
        const double epsilon = 1e-10;
        return Math.Abs(M11 - other.M11) < epsilon &&
               Math.Abs(M12 - other.M12) < epsilon &&
               Math.Abs(M13 - other.M13) < epsilon &&
               Math.Abs(M21 - other.M21) < epsilon &&
               Math.Abs(M22 - other.M22) < epsilon &&
               Math.Abs(M23 - other.M23) < epsilon &&
               Math.Abs(M31 - other.M31) < epsilon &&
               Math.Abs(M32 - other.M32) < epsilon &&
               Math.Abs(M33 - other.M33) < epsilon;
    }

    public override bool Equals(object? obj)
    {
        return obj is Matrix3x3 other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(M11);
        hash.Add(M12);
        hash.Add(M13);
        hash.Add(M21);
        hash.Add(M22);
        hash.Add(M23);
        hash.Add(M31);
        hash.Add(M32);
        hash.Add(M33);
        return hash.ToHashCode();
    }

    public static bool operator ==(Matrix3x3 left, Matrix3x3 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Matrix3x3 left, Matrix3x3 right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"[{M11:F2}, {M12:F2}, {M13:F2}; {M21:F2}, {M22:F2}, {M23:F2}; {M31:F2}, {M32:F2}, {M33:F2}]";
    }
}
