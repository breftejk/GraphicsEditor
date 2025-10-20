using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace GraphicsEditor.UI.Converters;

/// <summary>
/// Converts a uint color value (ARGB) to Avalonia Color.
/// </summary>
public class UintToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is uint argb)
        {
            byte a = (byte)((argb >> 24) & 0xFF);
            byte r = (byte)((argb >> 16) & 0xFF);
            byte g = (byte)((argb >> 8) & 0xFF);
            byte b = (byte)(argb & 0xFF);
            
            return Color.FromArgb(a, r, g, b);
        }
        
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;
        }
        
        return 0u;
    }
}

/// <summary>
/// Converts a uint color value to hex string.
/// </summary>
public class UintToHexConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is uint argb)
        {
            return $"#{argb:X8}";
        }
        return "#FF000000";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex)
        {
            hex = hex.TrimStart('#');
            if (uint.TryParse(hex, NumberStyles.HexNumber, null, out uint result))
            {
                // If no alpha provided, assume full opacity
                if (hex.Length == 6)
                {
                    result |= 0xFF000000;
                }
                return result;
            }
        }
        return 0xFF000000u;
    }
}

/// <summary>
/// Converts a nullable uint color value to Avalonia Color.
/// </summary>
public class NullableUintToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is uint argb)
        {
            byte a = (byte)((argb >> 24) & 0xFF);
            byte r = (byte)((argb >> 16) & 0xFF);
            byte g = (byte)((argb >> 8) & 0xFF);
            byte b = (byte)(argb & 0xFF);
            
            return Color.FromArgb(a, r, g, b);
        }
        
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;
        }
        
        return null;
    }
}

/// <summary>
/// Converts a nullable uint color value to hex string.
/// </summary>
public class NullableUintToHexConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is uint argb)
        {
            return $"#{argb:X8}";
        }
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            hex = hex.TrimStart('#');
            if (uint.TryParse(hex, NumberStyles.HexNumber, null, out uint result))
            {
                // If no alpha provided, assume full opacity
                if (hex.Length == 6)
                {
                    result |= 0xFF000000;
                }
                return (uint?)result;
            }
        }
        return null;
    }
}

/// <summary>
/// Converts an enum value to boolean for RadioButton binding.
/// </summary>
public class EnumToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        string enumValue = value.ToString() ?? "";
        string targetValue = parameter.ToString() ?? "";
        
        return enumValue.Equals(targetValue, StringComparison.OrdinalIgnoreCase);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter != null)
        {
            return Enum.Parse(targetType, parameter.ToString()!);
        }
        
        return BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converts null to false, non-null to true.
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

