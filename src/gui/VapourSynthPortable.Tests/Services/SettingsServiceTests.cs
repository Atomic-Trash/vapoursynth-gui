namespace VapourSynthPortable.Tests.Services;

public class SettingsServiceTests
{
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        _service = new SettingsService();
    }

    #region Instantiation Tests

    [Fact]
    public void Constructor_DoesNotThrow()
    {
        // Act & Assert
        var action = () => new SettingsService();
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_SetsProjectRoot()
    {
        // Act
        var service = new SettingsService();

        // Assert
        service.ProjectRoot.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Load Tests

    [Fact]
    public void Load_ReturnsNonNullSettings()
    {
        // Act
        var result = _service.Load();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<AppSettings>();
    }

    [Fact]
    public void Load_ReturnsDefaultSettings_WhenNoSettingsFile()
    {
        // Act - Load will return defaults if no file exists
        var result = _service.Load();

        // Assert - Should have default values
        result.OutputDirectory.Should().Be("dist");
        result.CacheDirectory.Should().Be("build");
        result.DefaultPluginSet.Should().Be("standard");
    }

    #endregion

    #region GetOutputPath Tests

    [Fact]
    public void GetOutputPath_ReturnsNonEmptyPath()
    {
        // Act
        var result = _service.GetOutputPath();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetOutputPath_ContainsProjectRoot()
    {
        // Act
        var result = _service.GetOutputPath();

        // Assert
        result.Should().StartWith(_service.ProjectRoot);
    }

    #endregion

    #region GetCachePath Tests

    [Fact]
    public void GetCachePath_ReturnsNonEmptyPath()
    {
        // Act
        var result = _service.GetCachePath();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetCachePath_ContainsProjectRoot()
    {
        // Act
        var result = _service.GetCachePath();

        // Assert
        result.Should().StartWith(_service.ProjectRoot);
    }

    #endregion

    #region GetCacheSize Tests

    [Fact]
    public void GetCacheSize_ReturnsZeroOrMore()
    {
        // Act
        var result = _service.GetCacheSize();

        // Assert
        result.Should().BeGreaterOrEqualTo(0);
    }

    #endregion

    #region ClearCache Tests

    [Fact]
    public void ClearCache_DoesNotThrow_WhenCacheDoesNotExist()
    {
        // Act & Assert - Should not throw even if cache directory doesn't exist
        var action = () => _service.ClearCache();
        action.Should().NotThrow();
    }

    #endregion

    #region AppSettings Model Tests

    [Fact]
    public void AppSettings_DefaultValues_AreCorrect()
    {
        // Act
        var settings = new AppSettings();

        // Assert
        settings.OutputDirectory.Should().Be("dist");
        settings.CacheDirectory.Should().Be("build");
        settings.DefaultPluginSet.Should().Be("standard");
        settings.PythonVersion.Should().Be("3.12.4");
        settings.VapourSynthVersion.Should().Be("R68");
    }

    [Fact]
    public void AppSettings_CanSetProperties()
    {
        // Arrange & Act
        var settings = new AppSettings
        {
            OutputDirectory = "output",
            CacheDirectory = "cache",
            DefaultPluginSet = "full",
            PythonVersion = "3.13.0",
            VapourSynthVersion = "R69"
        };

        // Assert
        settings.OutputDirectory.Should().Be("output");
        settings.CacheDirectory.Should().Be("cache");
        settings.DefaultPluginSet.Should().Be("full");
        settings.PythonVersion.Should().Be("3.13.0");
        settings.VapourSynthVersion.Should().Be("R69");
    }

    #endregion
}
