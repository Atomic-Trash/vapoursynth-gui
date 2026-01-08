using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace VapourSynthPortable.Helpers;

/// <summary>
/// Converts zero count to Visible, non-zero to Collapsed (for empty states)
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts empty string to Collapsed, non-empty to Visible
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return string.IsNullOrEmpty(str) ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts null to Visible, non-null to Collapsed (for placeholder states)
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to border brush (for selection highlighting)
/// </summary>
public class BoolToBorderBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)); // Blue selection
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverts a boolean value
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return !b;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return !b;
        }
        return false;
    }
}

/// <summary>
/// Converts null to false, non-null to true (for enabling buttons)
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool to Visibility (true = Visible, false = Collapsed)
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility v)
        {
            return v == Visibility.Visible;
        }
        return false;
    }
}

/// <summary>
/// Converts bool to Visibility (inverted: true = Collapsed, false = Visible)
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility v)
        {
            return v != Visibility.Visible;
        }
        return true;
    }
}

/// <summary>
/// Converts string to bool for RadioButton binding (compares with ConverterParameter)
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && parameter is string param)
        {
            return str.Equals(param, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b && parameter is string param)
        {
            return param;
        }
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts Color to SolidColorBrush
/// </summary>
public class ColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return new SolidColorBrush(color);
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            return brush.Color;
        }
        return Colors.Gray;
    }
}

/// <summary>
/// Converts frame count to pixel width based on zoom (static 5 pixels per frame * zoom)
/// </summary>
public class FrameToPixelConverter : IValueConverter
{
    private const double PixelsPerFrame = 5.0;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long frames)
        {
            // Default zoom is 1.0
            double zoom = 1.0;
            if (parameter is double z)
                zoom = z;

            return frames * PixelsPerFrame * zoom;
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double pixels)
        {
            double zoom = 1.0;
            if (parameter is double z)
                zoom = z;

            return (long)(pixels / (PixelsPerFrame * zoom));
        }
        return 0L;
    }
}

/// <summary>
/// Converts TrackType to mute button tooltip
/// </summary>
public class TrackTypeToMuteTooltipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is VapourSynthPortable.Models.TrackType trackType)
        {
            return trackType == VapourSynthPortable.Models.TrackType.Video ? "Hide" : "Mute";
        }
        return "Mute";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts enum value to bool for RadioButton binding
/// </summary>
public class EnumBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;

        var enumValue = value.ToString();
        var targetValue = parameter.ToString();
        return enumValue?.Equals(targetValue, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b && parameter != null)
        {
            return Enum.Parse(targetType, parameter.ToString()!);
        }
        return Binding.DoNothing;
    }
}

/// <summary>
/// Shows Visible when CompareMode is Wipe, Collapsed otherwise
/// </summary>
public class WipeVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is VapourSynthPortable.ViewModels.CompareDisplayMode mode)
        {
            return mode == VapourSynthPortable.ViewModels.CompareDisplayMode.Wipe
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean (IsFavorite) to gold star color or gray
/// </summary>
public class BoolToFavoriteColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isFavorite && isFavorite)
        {
            return new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)); // Gold
        }
        return new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)); // Gray
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
