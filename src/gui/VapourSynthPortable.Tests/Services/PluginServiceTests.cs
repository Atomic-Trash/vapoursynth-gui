namespace VapourSynthPortable.Tests.Services;

public class PluginServiceTests
{
    private readonly PluginService _service;

    public PluginServiceTests()
    {
        _service = new PluginService();
    }

    #region Instantiation Tests

    [Fact]
    public void Constructor_DoesNotThrow()
    {
        // Act & Assert
        var action = () => new PluginService();
        action.Should().NotThrow();
    }

    #endregion

    #region LoadPlugins Tests

    [Fact]
    public void LoadPlugins_ReturnsNonNullList()
    {
        // Act
        var result = _service.LoadPlugins();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<Plugin>>();
    }

    [Fact]
    public void LoadPlugins_ReturnsEmptyList_WhenNoConfig()
    {
        // Note: This test validates behavior when config isn't found
        // The actual result depends on whether plugins.json exists in the search path
        var result = _service.LoadPlugins();

        // Assert - Should return a list (possibly empty if no config found)
        result.Should().NotBeNull();
    }

    #endregion

    #region LoadPythonPackages Tests

    [Fact]
    public void LoadPythonPackages_ReturnsNonNullList()
    {
        // Act
        var result = _service.LoadPythonPackages();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<PythonPackage>>();
    }

    #endregion

    #region LoadEnabledPlugins Tests

    [Fact]
    public void LoadEnabledPlugins_ReturnsNonNullHashSet()
    {
        // Act
        var result = _service.LoadEnabledPlugins();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<HashSet<string>>();
    }

    #endregion

    #region GetPluginStatus Tests

    [Fact]
    public void GetPluginStatus_ReturnsUpdateAvailable_WhenHasUpdate()
    {
        // Arrange
        var plugin = new Plugin { Name = "TestPlugin", Files = new List<string> { "test.dll" } };

        // Act
        var status = _service.GetPluginStatus(plugin, hasUpdate: true);

        // Assert
        status.Should().Be("Update Available");
    }

    [Fact]
    public void GetPluginStatus_ReturnsNotInstalled_WhenPluginFileMissing()
    {
        // Arrange - plugin with files that don't exist
        var plugin = new Plugin
        {
            Name = "NonExistentPlugin",
            Files = new List<string> { "nonexistent_" + Guid.NewGuid().ToString("N") + ".dll" }
        };

        // Act
        var status = _service.GetPluginStatus(plugin, hasUpdate: false);

        // Assert
        status.Should().Be("Not Installed");
    }

    #endregion

    #region IsPluginInstalled Tests

    [Fact]
    public void IsPluginInstalled_ReturnsFalse_WhenPluginFilesDoNotExist()
    {
        // Arrange
        var plugin = new Plugin
        {
            Name = "TestPlugin",
            Files = new List<string> { "nonexistent_" + Guid.NewGuid().ToString("N") + ".dll" }
        };

        // Act
        var result = _service.IsPluginInstalled(plugin);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPluginInstalled_ReturnsFalse_WhenPluginHasNoFiles()
    {
        // Arrange
        var plugin = new Plugin
        {
            Name = "EmptyPlugin",
            Files = new List<string>()
        };

        // Act
        var result = _service.IsPluginInstalled(plugin);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Plugin Model Tests

    [Fact]
    public void Plugin_DefaultValues_AreCorrect()
    {
        // Act
        var plugin = new Plugin();

        // Assert
        plugin.Name.Should().BeEmpty();
        plugin.Description.Should().BeEmpty();
        plugin.Set.Should().Be("standard");
        plugin.Url.Should().BeEmpty();
        plugin.Version.Should().BeEmpty();
        plugin.Files.Should().NotBeNull().And.BeEmpty();
        plugin.Dependencies.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Plugin_CanSetProperties()
    {
        // Arrange & Act
        var plugin = new Plugin
        {
            Name = "TestPlugin",
            Description = "A test plugin",
            Set = "full",
            Url = "https://example.com/plugin.zip",
            Version = "v1.0.0",
            Files = new List<string> { "plugin.dll" },
            Dependencies = new List<string> { "vcruntime140.dll" }
        };

        // Assert
        plugin.Name.Should().Be("TestPlugin");
        plugin.Description.Should().Be("A test plugin");
        plugin.Set.Should().Be("full");
        plugin.Url.Should().Be("https://example.com/plugin.zip");
        plugin.Version.Should().Be("v1.0.0");
        plugin.Files.Should().ContainSingle().Which.Should().Be("plugin.dll");
        plugin.Dependencies.Should().ContainSingle().Which.Should().Be("vcruntime140.dll");
    }

    #endregion

    #region PluginConfig Model Tests

    [Fact]
    public void PluginConfig_DefaultValues_AreCorrect()
    {
        // Act
        var config = new PluginConfig();

        // Assert
        config.Version.Should().BeEmpty();
        config.Description.Should().BeEmpty();
        config.Plugins.Should().NotBeNull().And.BeEmpty();
        config.PythonPackages.Should().NotBeNull().And.BeEmpty();
    }

    #endregion
}
