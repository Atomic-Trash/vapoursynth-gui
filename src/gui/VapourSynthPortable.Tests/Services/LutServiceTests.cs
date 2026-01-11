using System.IO;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.Tests.Services;

public class LutServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LutService _service;

    public LutServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"LutServiceTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _service = new LutService();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch { }
    }

    private string CreateValidCubeLut(string name = "test", int size = 4)
    {
        var path = Path.Combine(_tempDir, $"{name}.cube");
        var lines = new List<string>
        {
            $"TITLE \"{name}\"",
            $"LUT_3D_SIZE {size}",
            "DOMAIN_MIN 0.0 0.0 0.0",
            "DOMAIN_MAX 1.0 1.0 1.0"
        };

        // Generate identity LUT data (size^3 entries)
        for (int b = 0; b < size; b++)
        {
            for (int g = 0; g < size; g++)
            {
                for (int r = 0; r < size; r++)
                {
                    var rv = r / (float)(size - 1);
                    var gv = g / (float)(size - 1);
                    var bv = b / (float)(size - 1);
                    lines.Add($"{rv:F6} {gv:F6} {bv:F6}");
                }
            }
        }

        File.WriteAllLines(path, lines);
        return path;
    }

    #region LoadLut Tests

    [Fact]
    public void LoadLut_ValidFile_ReturnsSuccess()
    {
        // Arrange
        var lutPath = CreateValidCubeLut();

        // Act
        var result = _service.LoadLut(lutPath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public void LoadLut_ValidFile_ParsesTitle()
    {
        // Arrange
        var lutPath = CreateValidCubeLut("MyCustomLut");

        // Act
        var result = _service.LoadLut(lutPath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("MyCustomLut", result.Value!.Title);
    }

    [Fact]
    public void LoadLut_ValidFile_ParsesSize()
    {
        // Arrange
        var lutPath = CreateValidCubeLut(size: 8);

        // Act
        var result = _service.LoadLut(lutPath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(8, result.Value!.Size);
    }

    [Fact]
    public void LoadLut_ValidFile_ParsesData()
    {
        // Arrange
        var lutPath = CreateValidCubeLut(size: 4);

        // Act
        var result = _service.LoadLut(lutPath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(4 * 4 * 4, result.Value!.Data.Length); // size^3
    }

    [Fact]
    public void LoadLut_EmptyPath_ReturnsFailure()
    {
        // Act
        var result = _service.LoadLut("");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("empty", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadLut_NullPath_ReturnsFailure()
    {
        // Act
        var result = _service.LoadLut(null!);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void LoadLut_NonExistentFile_ReturnsFailure()
    {
        // Act
        var result = _service.LoadLut(@"C:\nonexistent\path\lut.cube");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadLut_InvalidData_ReturnsFailure()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "invalid.cube");
        File.WriteAllText(path, "TITLE \"Invalid\"\nLUT_3D_SIZE 4\n0.0 0.0 0.0");

        // Act
        var result = _service.LoadLut(path);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void LoadLut_CachesResult()
    {
        // Arrange
        var lutPath = CreateValidCubeLut();

        // Act
        var result1 = _service.LoadLut(lutPath);
        var result2 = _service.LoadLut(lutPath);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Same(result1.Value, result2.Value); // Same cached instance
    }

    #endregion

    #region ClearCache Tests

    [Fact]
    public void ClearCache_ClearsLoadedLuts()
    {
        // Arrange
        var lutPath = CreateValidCubeLut();
        var result1 = _service.LoadLut(lutPath);
        var lut1 = result1.Value;

        // Act
        _service.ClearCache();
        var result2 = _service.LoadLut(lutPath);

        // Assert
        Assert.NotSame(lut1, result2.Value); // Different instance after cache clear
    }

    [Fact]
    public void ClearCache_DoesNotThrow()
    {
        // Act & Assert - should not throw
        _service.ClearCache();
        _service.ClearCache(); // Clear empty cache
    }

    #endregion

    #region Lut3D Tests

    [Fact]
    public void Lut3D_Sample_IdentityLutReturnsInput()
    {
        // Arrange
        var lutPath = CreateValidCubeLut(size: 8);
        var result = _service.LoadLut(lutPath);
        var lut = result.Value!;

        // Act - Sample identity LUT at various points
        var sample1 = lut.Sample(0.5f, 0.5f, 0.5f);
        var sample2 = lut.Sample(0.0f, 0.0f, 0.0f);
        var sample3 = lut.Sample(1.0f, 1.0f, 1.0f);

        // Assert - Identity LUT should return approximately the same values
        Assert.InRange(sample1.X, 0.45f, 0.55f);
        Assert.InRange(sample1.Y, 0.45f, 0.55f);
        Assert.InRange(sample1.Z, 0.45f, 0.55f);

        Assert.InRange(sample2.X, 0.0f, 0.05f);
        Assert.InRange(sample2.Y, 0.0f, 0.05f);
        Assert.InRange(sample2.Z, 0.0f, 0.05f);

        Assert.InRange(sample3.X, 0.95f, 1.0f);
        Assert.InRange(sample3.Y, 0.95f, 1.0f);
        Assert.InRange(sample3.Z, 0.95f, 1.0f);
    }

    [Fact]
    public void Lut3D_Sample_ClampsOutOfRangeValues()
    {
        // Arrange
        var lutPath = CreateValidCubeLut(size: 4);
        var result = _service.LoadLut(lutPath);
        var lut = result.Value!;

        // Act - Sample with out of range values
        var sample1 = lut.Sample(-0.5f, -0.5f, -0.5f);
        var sample2 = lut.Sample(1.5f, 1.5f, 1.5f);

        // Assert - Values should be clamped to valid range
        Assert.InRange(sample1.X, 0.0f, 0.1f);
        Assert.InRange(sample1.Y, 0.0f, 0.1f);
        Assert.InRange(sample1.Z, 0.0f, 0.1f);

        Assert.InRange(sample2.X, 0.9f, 1.0f);
        Assert.InRange(sample2.Y, 0.9f, 1.0f);
        Assert.InRange(sample2.Z, 0.9f, 1.0f);
    }

    [Fact]
    public void Lut3D_HasCorrectDefaults()
    {
        // Arrange & Act
        var lut = new Lut3D();

        // Assert
        Assert.Equal("", lut.Title);
        Assert.Equal(0, lut.Size);
        Assert.Equal(0f, lut.DomainMin);
        Assert.Equal(0f, lut.DomainMax);
        Assert.NotNull(lut.Data);
    }

    #endregion

    #region Vector3 Tests

    [Fact]
    public void Vector3_ConstructorSetsValues()
    {
        // Arrange & Act
        var v = new Vector3(0.5f, 0.75f, 1.0f);

        // Assert
        Assert.Equal(0.5f, v.X);
        Assert.Equal(0.75f, v.Y);
        Assert.Equal(1.0f, v.Z);
    }

    [Fact]
    public void Vector3_DefaultIsZero()
    {
        // Arrange & Act
        var v = new Vector3();

        // Assert
        Assert.Equal(0f, v.X);
        Assert.Equal(0f, v.Y);
        Assert.Equal(0f, v.Z);
    }

    #endregion

    #region Domain Tests

    [Fact]
    public void LoadLut_ParsesDomainMinMax()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "domain.cube");
        var lines = new List<string>
        {
            "TITLE \"Domain Test\"",
            "LUT_3D_SIZE 2",
            "DOMAIN_MIN 0.0 0.0 0.0",
            "DOMAIN_MAX 1.0 1.0 1.0"
        };

        // Generate 2x2x2 LUT data
        for (int i = 0; i < 8; i++)
        {
            lines.Add("0.5 0.5 0.5");
        }

        File.WriteAllLines(path, lines);

        // Act
        var result = _service.LoadLut(path);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0f, result.Value!.DomainMin);
        Assert.Equal(1f, result.Value!.DomainMax);
    }

    #endregion

    #region ApplyLut Tests

    [Fact]
    public void ApplyLut_NullSource_ReturnsSource()
    {
        // Arrange
        var lutPath = CreateValidCubeLut();
        var lut = _service.LoadLut(lutPath).Value!;

        // Act
        var result = _service.ApplyLut(null!, lut);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ApplyLut_NullLut_ReturnsSource()
    {
        // Note: This test would require a BitmapSource which needs WPF context
        // We'll just verify the null handling logic
        var result = _service.ApplyLut(null!, null!);
        Assert.Null(result);
    }

    #endregion
}
