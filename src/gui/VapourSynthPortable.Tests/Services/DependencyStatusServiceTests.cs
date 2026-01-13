using VapourSynthPortable.Services;

namespace VapourSynthPortable.Tests.Services;

/// <summary>
/// Tests for DependencyStatusService that checks for required dependencies.
/// </summary>
public class DependencyStatusServiceTests
{
    [Fact]
    public void Constructor_InitializesCurrentStatus()
    {
        // Arrange & Act
        var sut = new DependencyStatusService();

        // Assert
        Assert.NotNull(sut.CurrentStatus);
    }

    [Fact]
    public async Task CheckDependenciesAsync_ReturnsReport()
    {
        // Arrange
        var sut = new DependencyStatusService();

        // Act
        var report = await sut.CheckDependenciesAsync();

        // Assert
        Assert.NotNull(report);
        Assert.NotNull(report.VapourSynth);
        Assert.NotNull(report.FFmpeg);
        Assert.NotNull(report.Python);
        Assert.NotNull(report.LibMpv);
    }

    [Fact]
    public async Task CheckDependenciesAsync_SetsCheckedAtTimestamp()
    {
        // Arrange
        var sut = new DependencyStatusService();
        var beforeCheck = DateTime.UtcNow;

        // Act
        var report = await sut.CheckDependenciesAsync();

        // Assert
        Assert.True(report.CheckedAt >= beforeCheck);
    }

    [Fact]
    public async Task CheckDependenciesAsync_UpdatesCurrentStatus()
    {
        // Arrange
        var sut = new DependencyStatusService();

        // Act
        var report = await sut.CheckDependenciesAsync();

        // Assert
        Assert.Equal(report, sut.CurrentStatus);
    }

    [Fact]
    public async Task CheckDependenciesAsync_RaisesStatusChangedEvent()
    {
        // Arrange
        var sut = new DependencyStatusService();
        DependencyStatusReport? receivedReport = null;
        sut.StatusChanged += (sender, report) => receivedReport = report;

        // Act
        await sut.CheckDependenciesAsync();

        // Assert
        Assert.NotNull(receivedReport);
    }

    [Fact]
    public async Task CheckDependencyAsync_ReturnsResultForVapourSynth()
    {
        // Arrange
        var sut = new DependencyStatusService();

        // Act
        var result = await sut.CheckDependencyAsync("vapoursynth");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("VapourSynth", result.Value.Name);
    }

    [Fact]
    public async Task CheckDependencyAsync_ReturnsResultForFFmpeg()
    {
        // Arrange
        var sut = new DependencyStatusService();

        // Act
        var result = await sut.CheckDependencyAsync("ffmpeg");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("FFmpeg", result.Value.Name);
    }

    [Fact]
    public async Task CheckDependencyAsync_ReturnsFailureForUnknownDependency()
    {
        // Arrange
        var sut = new DependencyStatusService();

        // Act
        var result = await sut.CheckDependencyAsync("unknown");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void DependencyStatusReport_AllRequiredAvailable_WhenVSAndFFmpegAvailable()
    {
        // Arrange
        var report = new DependencyStatusReport
        {
            VapourSynth = DependencyStatus.Available("VapourSynth", "R68"),
            FFmpeg = DependencyStatus.Available("FFmpeg", "6.1"),
            Python = DependencyStatus.Unavailable("Python"),
            LibMpv = DependencyStatus.Unavailable("libmpv")
        };

        // Assert
        Assert.True(report.AllRequiredAvailable);
        Assert.False(report.AllAvailable);
    }

    [Fact]
    public void DependencyStatusReport_AllAvailable_WhenAllDependenciesAvailable()
    {
        // Arrange
        var report = new DependencyStatusReport
        {
            VapourSynth = DependencyStatus.Available("VapourSynth", "R68"),
            FFmpeg = DependencyStatus.Available("FFmpeg", "6.1"),
            Python = DependencyStatus.Available("Python", "3.12"),
            LibMpv = DependencyStatus.Available("libmpv", "2.0")
        };

        // Assert
        Assert.True(report.AllRequiredAvailable);
        Assert.True(report.AllAvailable);
    }

    [Fact]
    public void DependencyStatusReport_GetMissingRequired_ReturnsOnlyRequiredDependencies()
    {
        // Arrange
        var report = new DependencyStatusReport
        {
            VapourSynth = DependencyStatus.Unavailable("VapourSynth"),
            FFmpeg = DependencyStatus.Available("FFmpeg", "6.1"),
            Python = DependencyStatus.Unavailable("Python"),
            LibMpv = DependencyStatus.Unavailable("libmpv")
        };

        // Act
        var missing = report.GetMissingRequired();

        // Assert
        Assert.Single(missing);
        Assert.Equal("VapourSynth", missing[0].Name);
    }

    [Fact]
    public void DependencyStatusReport_GetAllMissing_ReturnsAllMissingDependencies()
    {
        // Arrange
        var report = new DependencyStatusReport
        {
            VapourSynth = DependencyStatus.Available("VapourSynth", "R68"),
            FFmpeg = DependencyStatus.Available("FFmpeg", "6.1"),
            Python = DependencyStatus.Unavailable("Python"),
            LibMpv = DependencyStatus.Unavailable("libmpv")
        };

        // Act
        var missing = report.GetAllMissing();

        // Assert
        Assert.Equal(2, missing.Count);
    }

    [Fact]
    public void DependencyStatus_Available_SetsProperties()
    {
        // Act
        var status = DependencyStatus.Available("TestDep", "1.0", @"C:\path");

        // Assert
        Assert.True(status.IsAvailable);
        Assert.Equal("TestDep", status.Name);
        Assert.Equal("1.0", status.Version);
        Assert.Equal(@"C:\path", status.Path);
    }

    [Fact]
    public void DependencyStatus_Unavailable_SetsProperties()
    {
        // Act
        var status = DependencyStatus.Unavailable("TestDep", "Not found");

        // Assert
        Assert.False(status.IsAvailable);
        Assert.Equal("TestDep", status.Name);
        Assert.Equal("Not found", status.ErrorMessage);
    }
}
