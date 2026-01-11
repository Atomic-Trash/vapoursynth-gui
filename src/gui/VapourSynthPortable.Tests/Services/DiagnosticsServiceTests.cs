using VapourSynthPortable.Services;

namespace VapourSynthPortable.Tests.Services;

public class DiagnosticsServiceTests
{
    #region Initialization Tests

    [Fact]
    public void Constructor_DoesNotThrow()
    {
        // Act & Assert - should not throw
        var service = new DiagnosticsService();
        Assert.NotNull(service);
    }

    #endregion

    #region RunFullDiagnostics Tests

    [Fact]
    public async Task RunFullDiagnostics_ReturnsValidReport()
    {
        // Arrange
        var service = new DiagnosticsService();

        // Act
        var report = await service.RunFullDiagnostics();

        // Assert
        Assert.NotNull(report);
        Assert.True(report.GeneratedAt <= DateTime.Now);
        Assert.NotEmpty(report.OsVersion);
        Assert.NotEmpty(report.DotNetVersion);
        Assert.True(report.DotNetOk); // .NET should always be OK since we're running
    }

    [Fact]
    public async Task RunFullDiagnostics_SetsIssuesForMissingComponents()
    {
        // Arrange
        var service = new DiagnosticsService();

        // Act
        var report = await service.RunFullDiagnostics();

        // Assert
        // In test environment, VapourSynth/Python may not be available
        // so either we have issues or all components are OK
        Assert.NotNull(report.Issues);
        Assert.NotNull(report.Recommendations);
    }

    [Fact]
    public async Task RunFullDiagnostics_ReturnsPluginList()
    {
        // Arrange
        var service = new DiagnosticsService();

        // Act
        var report = await service.RunFullDiagnostics();

        // Assert
        Assert.NotNull(report.Plugins);
        // Plugins list may be empty if dist directory doesn't exist
    }

    #endregion

    #region GetLoadedPlugins Tests

    [Fact]
    public async Task GetLoadedPlugins_ReturnsListEvenIfEmpty()
    {
        // Arrange
        var service = new DiagnosticsService();

        // Act
        var plugins = await service.GetLoadedPlugins();

        // Assert
        Assert.NotNull(plugins);
    }

    #endregion

    #region CheckVapourSynth Tests

    [Fact]
    public async Task CheckVapourSynth_ReturnsBooleanResult()
    {
        // Arrange
        var service = new DiagnosticsService();

        // Act
        var result = await service.CheckVapourSynth();

        // Assert - result should be boolean (may be true or false depending on environment)
        Assert.IsType<bool>(result);
    }

    #endregion

    #region CheckPython Tests

    [Fact]
    public async Task CheckPython_ReturnsBooleanResult()
    {
        // Arrange
        var service = new DiagnosticsService();

        // Act
        var result = await service.CheckPython();

        // Assert
        Assert.IsType<bool>(result);
    }

    #endregion

    #region GetGpuInfo Tests

    [Fact]
    public async Task GetGpuInfo_ReturnsNullOrValidGpuInfo()
    {
        // Arrange
        var service = new DiagnosticsService();

        // Act
        var gpuInfo = await service.GetGpuInfo();

        // Assert - may be null if no GPU or if wmic fails
        if (gpuInfo != null)
        {
            Assert.NotNull(gpuInfo.Name);
            Assert.NotNull(gpuInfo.DriverVersion);
        }
    }

    #endregion

    #region FormatReport Tests

    [Fact]
    public void FormatReport_ReturnsFormattedString()
    {
        // Arrange
        var report = new DiagnosticsReport
        {
            DotNetOk = true,
            DotNetVersion = ".NET 8.0",
            VapourSynthOk = false,
            VapourSynthVersion = "",
            PythonOk = true,
            PythonVersion = "Python 3.12",
            FFmpegOk = true,
            FFmpegVersion = "ffmpeg version 6.0",
            LibMpvOk = false,
            OsVersion = "Windows 11",
            Issues = ["VapourSynth not found"],
            Recommendations = ["Run Build-Portable.ps1"]
        };

        // Act
        var formatted = DiagnosticsService.FormatReport(report);

        // Assert
        Assert.Contains("VapourSynth Studio - System Diagnostic Report", formatted);
        Assert.Contains("[OK]", formatted);
        Assert.Contains("[!!]", formatted);
        Assert.Contains("VapourSynth not found", formatted);
        Assert.Contains("Build-Portable.ps1", formatted);
    }

    [Fact]
    public void FormatReport_IncludesAllComponentStatuses()
    {
        // Arrange
        var report = new DiagnosticsReport
        {
            DotNetOk = true,
            DotNetVersion = "Test",
            VapourSynthOk = true,
            PythonOk = true,
            FFmpegOk = true,
            LibMpvOk = true,
            Plugins = [new PluginInfo { Name = "TestPlugin" }]
        };

        // Act
        var formatted = DiagnosticsService.FormatReport(report);

        // Assert
        Assert.Contains(".NET SDK", formatted);
        Assert.Contains("VapourSynth", formatted);
        Assert.Contains("Python", formatted);
        Assert.Contains("FFmpeg", formatted);
        Assert.Contains("libmpv", formatted);
        Assert.Contains("Plugins", formatted);
    }

    [Fact]
    public void FormatReport_WithNoIssues_DoesNotShowIssuesSection()
    {
        // Arrange
        var report = new DiagnosticsReport
        {
            Issues = [],
            Recommendations = []
        };

        // Act
        var formatted = DiagnosticsService.FormatReport(report);

        // Assert
        Assert.DoesNotContain("Issues Found:", formatted);
    }

    [Fact]
    public void FormatReport_WithGpu_IncludesGpuInfo()
    {
        // Arrange
        var report = new DiagnosticsReport
        {
            Gpu = new GpuInfo
            {
                Name = "NVIDIA GeForce RTX 4090",
                SupportsNvenc = true
            }
        };

        // Act
        var formatted = DiagnosticsService.FormatReport(report);

        // Assert
        Assert.Contains("RTX 4090", formatted);
    }

    #endregion

    #region PluginInfo Tests

    [Fact]
    public void PluginInfo_HasCorrectDefaults()
    {
        // Arrange & Act
        var plugin = new PluginInfo();

        // Assert
        Assert.Equal("", plugin.Name);
        Assert.Equal("", plugin.FileName);
        Assert.Equal("", plugin.Path);
        Assert.Equal("", plugin.Version);
        Assert.False(plugin.IsLoaded);
    }

    #endregion

    #region GpuInfo Tests

    [Fact]
    public void GpuInfo_HasCorrectDefaults()
    {
        // Arrange & Act
        var gpu = new GpuInfo();

        // Assert
        Assert.Equal("", gpu.Name);
        Assert.Equal("", gpu.DriverVersion);
        Assert.Equal(0, gpu.AdapterRam);
        Assert.False(gpu.SupportsNvenc);
        Assert.False(gpu.SupportsAmf);
        Assert.False(gpu.SupportsQsv);
    }

    [Theory]
    [InlineData("NVIDIA GeForce RTX 3080", true, false, false)]
    [InlineData("AMD Radeon RX 6900 XT", false, true, false)]
    [InlineData("Intel UHD Graphics 630", false, false, true)]
    public void GpuInfo_SupportsFlags_SetCorrectly(string gpuName, bool nvenc, bool amf, bool qsv)
    {
        // Arrange
        var gpu = new GpuInfo
        {
            Name = gpuName,
            SupportsNvenc = gpuName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase),
            SupportsAmf = gpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                         gpuName.Contains("Radeon", StringComparison.OrdinalIgnoreCase),
            SupportsQsv = gpuName.Contains("Intel", StringComparison.OrdinalIgnoreCase)
        };

        // Assert
        Assert.Equal(nvenc, gpu.SupportsNvenc);
        Assert.Equal(amf, gpu.SupportsAmf);
        Assert.Equal(qsv, gpu.SupportsQsv);
    }

    #endregion

    #region DiagnosticsReport Tests

    [Fact]
    public void DiagnosticsReport_HasCorrectDefaults()
    {
        // Arrange & Act
        var report = new DiagnosticsReport();

        // Assert
        Assert.True(report.GeneratedAt <= DateTime.Now);
        Assert.False(report.DotNetOk);
        Assert.Equal("", report.DotNetVersion);
        Assert.Empty(report.Plugins);
        Assert.Null(report.Gpu);
        Assert.Empty(report.Issues);
        Assert.Empty(report.Recommendations);
    }

    [Fact]
    public void DiagnosticsReport_IsRecord_CanUseWith()
    {
        // Arrange
        var original = new DiagnosticsReport { DotNetOk = false };

        // Act
        var modified = original with { DotNetOk = true };

        // Assert
        Assert.False(original.DotNetOk);
        Assert.True(modified.DotNetOk);
    }

    #endregion
}
