using System.Collections.ObjectModel;
using Moq;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Tests.ViewModels;

public class SettingsViewModelTests
{
    private static Mock<ISettingsService> CreateMockSettingsService()
    {
        var mock = new Mock<ISettingsService>();
        mock.Setup(m => m.Load()).Returns(new AppSettings());
        mock.Setup(m => m.GetCacheSize()).Returns(0);
        mock.Setup(m => m.ProjectRoot).Returns(@"C:\TestProject");
        return mock;
    }

    private static Mock<IVapourSynthService> CreateMockVsService(bool isAvailable = true)
    {
        var mock = new Mock<IVapourSynthService>();
        mock.Setup(m => m.IsAvailable).Returns(isAvailable);
        mock.Setup(m => m.GetPluginsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<VapourSynthPlugin>());
        return mock;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesCollections()
    {
        // Arrange & Act
        var vm = new SettingsViewModel(
            CreateMockSettingsService().Object,
            CreateMockVsService().Object);

        // Assert
        Assert.NotNull(vm.PluginSets);
        Assert.NotNull(vm.ExportFormats);
        Assert.NotNull(vm.VideoCodecs);
        Assert.NotNull(vm.AudioCodecs);
        Assert.NotNull(vm.GpuPreferences);
        Assert.NotNull(vm.AppThemes);
        Assert.NotNull(vm.InstalledPlugins);
    }

    [Fact]
    public void Constructor_LoadsSettingsFromService()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var settings = new AppSettings
        {
            DefaultExportFormat = "mkv",
            DefaultVideoCodec = "libx265",
            DefaultAudioCodec = "flac",
            DefaultVideoQuality = 18,
            MaxCacheSizeMB = 2048
        };
        settingsService.Setup(m => m.Load()).Returns(settings);

        // Act
        var vm = new SettingsViewModel(settingsService.Object, CreateMockVsService().Object);

        // Assert
        Assert.Equal("mkv", vm.DefaultExportFormat);
        Assert.Equal("libx265", vm.DefaultVideoCodec);
        Assert.Equal("flac", vm.DefaultAudioCodec);
        Assert.Equal(18, vm.DefaultVideoQuality);
        Assert.Equal(2048, vm.MaxCacheSizeMB);
    }

    [Fact]
    public void PluginSets_ContainsExpectedValues()
    {
        // Arrange & Act
        var vm = new SettingsViewModel(
            CreateMockSettingsService().Object,
            CreateMockVsService().Object);

        // Assert
        Assert.Contains("minimal", vm.PluginSets);
        Assert.Contains("standard", vm.PluginSets);
        Assert.Contains("full", vm.PluginSets);
    }

    [Fact]
    public void ExportFormats_ContainsExpectedValues()
    {
        // Arrange & Act
        var vm = new SettingsViewModel(
            CreateMockSettingsService().Object,
            CreateMockVsService().Object);

        // Assert
        Assert.Contains("mp4", vm.ExportFormats);
        Assert.Contains("mkv", vm.ExportFormats);
        Assert.Contains("mov", vm.ExportFormats);
        Assert.Contains("avi", vm.ExportFormats);
        Assert.Contains("webm", vm.ExportFormats);
    }

    [Fact]
    public void VideoCodecs_ContainsExpectedValues()
    {
        // Arrange & Act
        var vm = new SettingsViewModel(
            CreateMockSettingsService().Object,
            CreateMockVsService().Object);

        // Assert
        Assert.Contains("libx264", vm.VideoCodecs);
        Assert.Contains("libx265", vm.VideoCodecs);
        Assert.Contains("h264_nvenc", vm.VideoCodecs);
        Assert.Contains("hevc_nvenc", vm.VideoCodecs);
    }

    #endregion

    #region Save Command Tests

    [Fact]
    public void SaveCommand_SavesSettingsToService()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new SettingsViewModel(settingsService.Object, CreateMockVsService().Object)
        {
            DefaultExportFormat = "mkv",
            DefaultVideoCodec = "libx265",
            DefaultVideoQuality = 18
        };

        // Act
        vm.SaveCommand.Execute(null);

        // Assert
        settingsService.Verify(s => s.Save(It.Is<AppSettings>(a =>
            a.DefaultExportFormat == "mkv" &&
            a.DefaultVideoCodec == "libx265" &&
            a.DefaultVideoQuality == 18)), Times.Once);
    }

    [Fact]
    public void SaveCommand_InvokesCloseAction()
    {
        // Arrange
        var closeActionCalled = false;
        var vm = new SettingsViewModel(
            CreateMockSettingsService().Object,
            CreateMockVsService().Object,
            () => closeActionCalled = true);

        // Act
        vm.SaveCommand.Execute(null);

        // Assert
        Assert.True(closeActionCalled);
    }

    #endregion

    #region Cancel Command Tests

    [Fact]
    public void CancelCommand_InvokesCloseAction()
    {
        // Arrange
        var closeActionCalled = false;
        var vm = new SettingsViewModel(
            CreateMockSettingsService().Object,
            CreateMockVsService().Object,
            () => closeActionCalled = true);

        // Act
        vm.CancelCommand.Execute(null);

        // Assert
        Assert.True(closeActionCalled);
    }

    [Fact]
    public void CancelCommand_DoesNotSaveSettings()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new SettingsViewModel(settingsService.Object, CreateMockVsService().Object);
        vm.DefaultExportFormat = "mkv"; // Change a setting

        // Act
        vm.CancelCommand.Execute(null);

        // Assert - Save should not be called on cancel
        settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.Never);
    }

    #endregion

    #region ResetToDefaults Command Tests

    [Fact]
    public void ResetToDefaultsCommand_ResetsExportSettings()
    {
        // Arrange
        var vm = new SettingsViewModel(
            CreateMockSettingsService().Object,
            CreateMockVsService().Object)
        {
            DefaultExportFormat = "mkv",
            DefaultVideoCodec = "libx265",
            DefaultVideoQuality = 10
        };

        // Act
        vm.ResetToDefaultsCommand.Execute(null);

        // Assert - Should reset to AppSettings defaults
        var defaults = new AppSettings();
        Assert.Equal(defaults.DefaultExportFormat, vm.DefaultExportFormat);
        Assert.Equal(defaults.DefaultVideoCodec, vm.DefaultVideoCodec);
        Assert.Equal(defaults.DefaultVideoQuality, vm.DefaultVideoQuality);
    }

    [Fact]
    public void ResetToDefaultsCommand_ResetsCacheSettings()
    {
        // Arrange
        var vm = new SettingsViewModel(
            CreateMockSettingsService().Object,
            CreateMockVsService().Object)
        {
            MaxCacheSizeMB = 4096,
            AutoClearCache = true
        };

        // Act
        vm.ResetToDefaultsCommand.Execute(null);

        // Assert
        var defaults = new AppSettings();
        Assert.Equal(defaults.MaxCacheSizeMB, vm.MaxCacheSizeMB);
        Assert.Equal(defaults.AutoClearCache, vm.AutoClearCache);
    }

    #endregion

    #region ClearCache Command Tests

    [Fact]
    public void ClearCacheCommand_CallsClearCacheOnService()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new SettingsViewModel(settingsService.Object, CreateMockVsService().Object);

        // Act
        vm.ClearCacheCommand.Execute(null);

        // Assert
        settingsService.Verify(s => s.ClearCache(), Times.Once);
    }

    [Fact]
    public void ClearCacheCommand_UpdatesCacheSize()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        settingsService.SetupSequence(s => s.GetCacheSize())
            .Returns(1024 * 1024 * 100) // 100 MB initially
            .Returns(0); // 0 after clear

        var vm = new SettingsViewModel(settingsService.Object, CreateMockVsService().Object);
        var initialSize = vm.CacheSize;

        // Act
        vm.ClearCacheCommand.Execute(null);

        // Assert - CacheSize should update after clearing
        settingsService.Verify(s => s.GetCacheSize(), Times.AtLeast(2));
    }

    #endregion

    #region Cache Size Formatting Tests

    [Theory]
    [InlineData(512, "512 B")]
    [InlineData(1024 * 50, "50.0 KB")]
    [InlineData(1024 * 1024 * 100, "100.0 MB")]
    [InlineData(1024L * 1024L * 1024L * 2, "2.00 GB")]
    public void CacheSize_FormatsCorrectly(long bytes, string expected)
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        settingsService.Setup(s => s.GetCacheSize()).Returns(bytes);

        // Act
        var vm = new SettingsViewModel(settingsService.Object, CreateMockVsService().Object);

        // Assert
        Assert.Equal(expected, vm.CacheSize);
    }

    #endregion

    #region Plugin Loading Tests

    [Fact]
    public async Task RefreshPluginsCommand_LoadsPlugins()
    {
        // Arrange
        var vsService = CreateMockVsService();
        var plugins = new List<VapourSynthPlugin>
        {
            new() { Namespace = "vs", Name = "Core" },
            new() { Namespace = "std", Name = "Standard" }
        };
        vsService.Setup(s => s.GetPluginsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(plugins);

        var vm = new SettingsViewModel(CreateMockSettingsService().Object, vsService.Object);

        // Act
        await Task.Delay(100); // Allow initial load
        vm.RefreshPluginsCommand.Execute(null);
        await Task.Delay(100); // Allow refresh

        // Assert
        vsService.Verify(s => s.GetPluginsAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public void VapourSynthAvailable_WhenServiceNotAvailable_ReturnsFalse()
    {
        // Arrange
        var vsService = CreateMockVsService(isAvailable: false);

        // Act
        var vm = new SettingsViewModel(CreateMockSettingsService().Object, vsService.Object);

        // Give time for async plugin loading to complete
        Thread.Sleep(200);

        // Assert
        Assert.False(vm.VapourSynthAvailable);
    }

    #endregion

    #region Property Default Values Tests

    [Fact]
    public void DefaultPluginSet_DefaultValue()
    {
        // Arrange & Act
        var vm = new SettingsViewModel(
            CreateMockSettingsService().Object,
            CreateMockVsService().Object);

        // Assert - Should match AppSettings default
        var defaults = new AppSettings();
        Assert.Equal(defaults.DefaultPluginSet, vm.DefaultPluginSet);
    }

    [Fact]
    public void AutoSaveEnabled_DefaultValue()
    {
        // Arrange & Act
        var vm = new SettingsViewModel(
            CreateMockSettingsService().Object,
            CreateMockVsService().Object);

        // Assert - Should match AppSettings default
        var defaults = new AppSettings();
        Assert.Equal(defaults.AutoSaveEnabled, vm.AutoSaveEnabled);
    }

    [Fact]
    public void TimelineZoom_DefaultValue()
    {
        // Arrange & Act
        var vm = new SettingsViewModel(
            CreateMockSettingsService().Object,
            CreateMockVsService().Object);

        // Assert - Should match AppSettings default
        var defaults = new AppSettings();
        Assert.Equal(defaults.TimelineZoom, vm.TimelineZoom);
    }

    #endregion
}
