using System.Globalization;
using System.IO;
using System.Windows.Controls;

namespace VapourSynthPortable.Helpers;

/// <summary>
/// Validates that a value is a positive integer within optional bounds.
/// </summary>
public class PositiveIntegerRule : ValidationRule
{
    public int Minimum { get; set; } = 1;
    public int Maximum { get; set; } = int.MaxValue;
    public string FieldName { get; set; } = "Value";

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return new ValidationResult(false, $"{FieldName} is required.");
        }

        if (!int.TryParse(value.ToString(), out var intValue))
        {
            return new ValidationResult(false, $"{FieldName} must be a whole number.");
        }

        if (intValue < Minimum)
        {
            return new ValidationResult(false, $"{FieldName} must be at least {Minimum}.");
        }

        if (intValue > Maximum)
        {
            return new ValidationResult(false, $"{FieldName} cannot exceed {Maximum}.");
        }

        return ValidationResult.ValidResult;
    }
}

/// <summary>
/// Validates that a path string is a valid directory path.
/// Does not require the directory to exist, just validates the format.
/// </summary>
public class DirectoryPathRule : ValidationRule
{
    public bool AllowEmpty { get; set; } = false;
    public bool RequireExists { get; set; } = false;
    public string FieldName { get; set; } = "Directory";

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        var path = value?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            return AllowEmpty
                ? ValidationResult.ValidResult
                : new ValidationResult(false, $"{FieldName} is required.");
        }

        // Check for invalid path characters
        try
        {
            var invalidChars = Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalidChars) >= 0)
            {
                return new ValidationResult(false, $"{FieldName} contains invalid characters.");
            }

            // Try to get full path to validate format
            var fullPath = Path.GetFullPath(path);

            // Check if path is rooted (absolute path)
            if (!Path.IsPathRooted(path))
            {
                return new ValidationResult(false, $"{FieldName} must be an absolute path.");
            }

            // If we require the directory to exist, check it
            if (RequireExists && !Directory.Exists(path))
            {
                return new ValidationResult(false, $"{FieldName} does not exist.");
            }
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
        {
            return new ValidationResult(false, $"{FieldName} is not a valid path.");
        }

        return ValidationResult.ValidResult;
    }
}

/// <summary>
/// Validates that a value is a positive decimal/double within optional bounds.
/// </summary>
public class PositiveDecimalRule : ValidationRule
{
    public double Minimum { get; set; } = 0.01;
    public double Maximum { get; set; } = double.MaxValue;
    public string FieldName { get; set; } = "Value";

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return new ValidationResult(false, $"{FieldName} is required.");
        }

        if (!double.TryParse(value.ToString(), NumberStyles.Any, cultureInfo, out var doubleValue))
        {
            return new ValidationResult(false, $"{FieldName} must be a number.");
        }

        if (doubleValue < Minimum)
        {
            return new ValidationResult(false, $"{FieldName} must be at least {Minimum}.");
        }

        if (doubleValue > Maximum)
        {
            return new ValidationResult(false, $"{FieldName} cannot exceed {Maximum}.");
        }

        return ValidationResult.ValidResult;
    }
}
