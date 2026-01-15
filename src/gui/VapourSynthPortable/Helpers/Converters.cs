using System.ComponentModel;
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
        return Binding.DoNothing;
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
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts null to Visible, non-null to Collapsed (for placeholder states)
/// Supports "Invert" parameter to swap behavior
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
        bool isNull = value == null;

        if (invert)
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        return isNull ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
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
        return Binding.DoNothing;
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
        return Binding.DoNothing;
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
        return Binding.DoNothing;
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
        return Binding.DoNothing;
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
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts ListSortDirection to arrow icon (for sort headers)
/// </summary>
public class SortDirectionToArrowConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ListSortDirection direction)
        {
            return direction == ListSortDirection.Ascending ? "\uE70E" : "\uE70D"; // Up / Down arrow
        }
        return "\uE70E"; // Default up
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts enum value to Visibility (Visible if matches parameter, Collapsed otherwise)
/// </summary>
public class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return Visibility.Collapsed;

        var enumValue = value.ToString();
        var targetValue = parameter.ToString();
        var matches = enumValue?.Equals(targetValue, StringComparison.OrdinalIgnoreCase) ?? false;
        return matches ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts string to Visibility (Visible if matches parameter, Collapsed otherwise)
/// </summary>
public class StringToVisibilityMatchConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && parameter is string param)
        {
            return str.Equals(param, StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts bool (IsFavorite) to star icon (filled or outline)
/// </summary>
public class BoolToFavoriteIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool isFavorite && isFavorite ? "\uE735" : "\uE734"; // Filled star / outline star
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// Shows Visible when string is null or empty, Collapsed otherwise (for placeholders)
/// </summary>
public class NullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return string.IsNullOrEmpty(str) ? Visibility.Visible : Visibility.Collapsed;
        }
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts count to visibility inverse (Visible when count > 0, Collapsed when 0)
/// </summary>
public class CountToVisibilityInverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts TimeSpan to Visibility (Visible if > 0, Collapsed otherwise)
/// </summary>
public class TimeSpanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan ts)
        {
            return ts.TotalSeconds > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// MultiValueConverter for wipe mode rectangle clipping
/// </summary>
public class WipeRectConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 3 &&
            values[0] is double width &&
            values[1] is double height &&
            values[2] is double position)
        {
            // Wipe from left to right: show processed frame from left edge to wipe position
            var wipeX = width * position;
            return new Rect(0, 0, wipeX, height);
        }
        return new Rect(0, 0, 0, 0);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return Array.Empty<object>();
    }
}

/// <summary>
/// Converts wipe position and container width to margin for wipe line indicator
/// </summary>
public class WipeLineMarginConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 &&
            values[0] is double width &&
            values[1] is double position)
        {
            var x = width * position;
            return new Thickness(x, 0, 0, 0);
        }
        return new Thickness(0);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return Array.Empty<object>();
    }
}

/// <summary>
/// Converts ProcessingStatus to background color
/// </summary>
public class StatusToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is VapourSynthPortable.Models.ProcessingStatus status)
        {
            return status switch
            {
                VapourSynthPortable.Models.ProcessingStatus.Pending => new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                VapourSynthPortable.Models.ProcessingStatus.Processing => new SolidColorBrush(Color.FromRgb(0x1A, 0x3A, 0x5C)),
                VapourSynthPortable.Models.ProcessingStatus.Completed => new SolidColorBrush(Color.FromRgb(0x1A, 0x3D, 0x2A)),
                VapourSynthPortable.Models.ProcessingStatus.Failed => new SolidColorBrush(Color.FromRgb(0x4A, 0x1A, 0x1A)),
                VapourSynthPortable.Models.ProcessingStatus.Cancelled => new SolidColorBrush(Color.FromRgb(0x3D, 0x3D, 0x1A)),
                _ => new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33))
            };
        }
        return new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts ProcessingStatus to foreground color
/// </summary>
public class StatusToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is VapourSynthPortable.Models.ProcessingStatus status)
        {
            return status switch
            {
                VapourSynthPortable.Models.ProcessingStatus.Pending => new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
                VapourSynthPortable.Models.ProcessingStatus.Processing => new SolidColorBrush(Color.FromRgb(0x6A, 0xB0, 0xFF)),
                VapourSynthPortable.Models.ProcessingStatus.Completed => new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80)),
                VapourSynthPortable.Models.ProcessingStatus.Failed => new SolidColorBrush(Color.FromRgb(0xE7, 0x48, 0x56)),
                VapourSynthPortable.Models.ProcessingStatus.Cancelled => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),
                _ => new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA))
            };
        }
        return new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts bool (IsPaused) to pause/play icon
/// </summary>
public class BoolToPauseIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool isPaused && isPaused ? "\uE768" : "\uE769"; // Play / Pause
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts bool (IsPaused) to color
/// </summary>
public class BoolToPauseColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool isPaused && isPaused
            ? new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)) // Amber when paused
            : new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)); // Gray when running
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts bool (IsPaused) to tooltip text
/// </summary>
public class BoolToPauseTooltipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool isPaused && isPaused ? "Resume processing" : "Pause processing";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts bool (IsPaused) to accessibility label
/// </summary>
public class BoolToPauseLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool isPaused && isPaused ? "Resume Queue" : "Pause Queue";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts bool (ShowOriginalInToggle) to label text
/// </summary>
public class BoolToToggleLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool showOriginal && showOriginal
            ? "ORIGINAL (click to toggle)"
            : "PROCESSED (click to toggle)";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts bool (ShowOriginalInToggle) to color
/// </summary>
public class BoolToToggleColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool showOriginal && showOriginal
            ? new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E)) // Gray for original
            : new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80)); // Green for processed
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts string to double for slider binding
/// </summary>
public class StringToDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
        {
            return result;
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return d.ToString(CultureInfo.InvariantCulture);
        }
        return "0";
    }
}

/// <summary>
/// Converts string "True"/"False" to bool for checkbox binding
/// </summary>
public class StringToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return str.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                   str.Equals("1", StringComparison.Ordinal);
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? "True" : "False";
        }
        return "False";
    }
}

/// <summary>
/// Converts export source type string to Segoe MDL2 icon
/// </summary>
public class SourceTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string sourceType)
        {
            return sourceType switch
            {
                "Timeline" => "\uE8A1",      // Edit icon for timeline
                "Source File" => "\uE8B7",   // Document icon for file
                _ => "\uE8B7"                // Default to document
            };
        }
        return "\uE8B7";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
