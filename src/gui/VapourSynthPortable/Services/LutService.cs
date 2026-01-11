using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;

namespace VapourSynthPortable.Services;

/// <summary>
/// Service for loading and applying 3D LUT files (.cube format)
/// </summary>
public class LutService
{
    private static readonly ILogger<LutService> _logger = LoggingService.GetLogger<LutService>();

    private readonly Dictionary<string, Lut3D> _lutCache = new();
    private readonly object _cacheLock = new();

    /// <summary>
    /// Load a .cube LUT file
    /// </summary>
    public Result<Lut3D> LoadLut(string path)
    {
        if (string.IsNullOrEmpty(path))
            return Result<Lut3D>.Failure("LUT path is empty");

        if (!File.Exists(path))
            return Result<Lut3D>.Failure("LUT file not found", path);

        lock (_cacheLock)
        {
            if (_lutCache.TryGetValue(path, out var cached))
                return Result<Lut3D>.Success(cached);
        }

        try
        {
            var lut = ParseCubeLut(path);
            if (lut == null)
            {
                return Result<Lut3D>.Failure("Failed to parse LUT file", $"Invalid format or data in {Path.GetFileName(path)}");
            }

            lock (_cacheLock)
            {
                _lutCache[path] = lut;
            }
            _logger.LogInformation("Loaded LUT: {Path} ({Size}x{Size}x{Size})", path, lut.Size, lut.Size, lut.Size);
            return Result<Lut3D>.Success(lut);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load LUT: {Path}", path);
            return Result<Lut3D>.Failure(ex, "Failed to load LUT file");
        }
    }

    /// <summary>
    /// Parse a .cube LUT file
    /// </summary>
    private static Lut3D? ParseCubeLut(string path)
    {
        var lines = File.ReadAllLines(path);
        int size = 0;
        float domainMin = 0f;
        float domainMax = 1f;
        var data = new List<Vector3>();
        string title = Path.GetFileNameWithoutExtension(path);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            // Parse metadata
            if (trimmed.StartsWith("TITLE", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmed.Split([' ', '"'], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                    title = string.Join(" ", parts.Skip(1)).Trim('"');
                continue;
            }

            if (trimmed.StartsWith("LUT_3D_SIZE", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1 && int.TryParse(parts[1], out var s))
                    size = s;
                continue;
            }

            if (trimmed.StartsWith("DOMAIN_MIN", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var min))
                    domainMin = min;
                continue;
            }

            if (trimmed.StartsWith("DOMAIN_MAX", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var max))
                    domainMax = max;
                continue;
            }

            // Parse LUT data (R G B values)
            var values = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (values.Length >= 3 &&
                float.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var r) &&
                float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var g) &&
                float.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var b))
            {
                data.Add(new Vector3(r, g, b));
            }
        }

        if (size == 0 || data.Count != size * size * size)
        {
            _logger.LogWarning("Invalid LUT file: {Path}. Size={Size}, DataCount={DataCount}, Expected={Expected}",
                path, size, data.Count, size * size * size);
            return null;
        }

        return new Lut3D
        {
            Title = title,
            Size = size,
            DomainMin = domainMin,
            DomainMax = domainMax,
            Data = data.ToArray()
        };
    }

    /// <summary>
    /// Apply a LUT to a BitmapSource
    /// </summary>
    public BitmapSource? ApplyLut(BitmapSource source, Lut3D lut, double intensity = 1.0)
    {
        if (source == null || lut == null)
            return source;

        try
        {
            // Convert to 32-bit BGRA for processing
            var formatConvertedBitmap = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            var width = formatConvertedBitmap.PixelWidth;
            var height = formatConvertedBitmap.PixelHeight;
            var stride = width * 4;
            var pixels = new byte[height * stride];
            formatConvertedBitmap.CopyPixels(pixels, stride, 0);

            // Apply LUT to each pixel
            for (int i = 0; i < pixels.Length; i += 4)
            {
                // BGRA format
                var b = pixels[i] / 255f;
                var g = pixels[i + 1] / 255f;
                var r = pixels[i + 2] / 255f;
                // a = pixels[i + 3] - preserve alpha

                // Sample LUT with trilinear interpolation
                var lutColor = lut.Sample(r, g, b);

                // Blend with original based on intensity
                if (intensity < 1.0)
                {
                    lutColor = new Vector3(
                        (float)(r + (lutColor.X - r) * intensity),
                        (float)(g + (lutColor.Y - g) * intensity),
                        (float)(b + (lutColor.Z - b) * intensity));
                }

                // Write back (clamped to 0-255)
                pixels[i] = (byte)Math.Clamp(lutColor.Z * 255, 0, 255);     // B
                pixels[i + 1] = (byte)Math.Clamp(lutColor.Y * 255, 0, 255); // G
                pixels[i + 2] = (byte)Math.Clamp(lutColor.X * 255, 0, 255); // R
            }

            // Create output bitmap
            var result = BitmapSource.Create(width, height, source.DpiX, source.DpiY,
                PixelFormats.Bgra32, null, pixels, stride);
            result.Freeze();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply LUT");
            return source;
        }
    }

    /// <summary>
    /// Clear the LUT cache
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _lutCache.Clear();
        }
        _logger.LogInformation("LUT cache cleared");
    }
}

/// <summary>
/// Represents a 3D LUT (Look-Up Table)
/// </summary>
public class Lut3D
{
    public string Title { get; init; } = "";
    public int Size { get; init; }
    public float DomainMin { get; init; }
    public float DomainMax { get; init; }
    public Vector3[] Data { get; init; } = [];

    /// <summary>
    /// Sample the LUT with trilinear interpolation
    /// </summary>
    public Vector3 Sample(float r, float g, float b)
    {
        // Normalize to LUT domain
        var scale = (Size - 1) / (DomainMax - DomainMin);
        var rScaled = (r - DomainMin) * scale;
        var gScaled = (g - DomainMin) * scale;
        var bScaled = (b - DomainMin) * scale;

        // Clamp to valid range
        rScaled = Math.Clamp(rScaled, 0, Size - 1);
        gScaled = Math.Clamp(gScaled, 0, Size - 1);
        bScaled = Math.Clamp(bScaled, 0, Size - 1);

        // Get integer indices and fractional parts
        var r0 = (int)rScaled;
        var g0 = (int)gScaled;
        var b0 = (int)bScaled;
        var r1 = Math.Min(r0 + 1, Size - 1);
        var g1 = Math.Min(g0 + 1, Size - 1);
        var b1 = Math.Min(b0 + 1, Size - 1);

        var rFrac = rScaled - r0;
        var gFrac = gScaled - g0;
        var bFrac = bScaled - b0;

        // Trilinear interpolation
        var c000 = GetValue(r0, g0, b0);
        var c001 = GetValue(r0, g0, b1);
        var c010 = GetValue(r0, g1, b0);
        var c011 = GetValue(r0, g1, b1);
        var c100 = GetValue(r1, g0, b0);
        var c101 = GetValue(r1, g0, b1);
        var c110 = GetValue(r1, g1, b0);
        var c111 = GetValue(r1, g1, b1);

        // Interpolate along R axis
        var c00 = Lerp(c000, c100, rFrac);
        var c01 = Lerp(c001, c101, rFrac);
        var c10 = Lerp(c010, c110, rFrac);
        var c11 = Lerp(c011, c111, rFrac);

        // Interpolate along G axis
        var c0 = Lerp(c00, c10, gFrac);
        var c1 = Lerp(c01, c11, gFrac);

        // Interpolate along B axis
        return Lerp(c0, c1, bFrac);
    }

    private Vector3 GetValue(int r, int g, int b)
    {
        // .cube files are stored in B-G-R order (blue changes fastest)
        var index = r + g * Size + b * Size * Size;
        return index < Data.Length ? Data[index] : new Vector3(0, 0, 0);
    }

    private static Vector3 Lerp(Vector3 a, Vector3 b, float t)
    {
        return new Vector3(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t);
    }
}

/// <summary>
/// Simple 3D vector for RGB color values
/// </summary>
public readonly struct Vector3
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; }

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
