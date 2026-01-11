using System.Collections.ObjectModel;
using Moq;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Tests.ViewModels;

public class ExportViewModelTests
{
    private static Mock<IMediaPoolService> CreateMockMediaPool()
    {
        var mock = new Mock<IMediaPoolService>();
        mock.Setup(m => m.MediaPool).Returns(new ObservableCollection<MediaItem>());
        mock.Setup(m => m.CurrentSource).Returns((MediaItem?)null);
        mock.Setup(m => m.HasSource).Returns(false);
        return mock;
    }

    private static Mock<ISettingsService> CreateMockSettingsService()
    {
        var mock = new Mock<ISettingsService>();
        mock.Setup(m => m.Load()).Returns(new AppSettings());
        return mock;
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_InitializesDefaultValues()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert - verify key properties are set and collections are populated
        Assert.NotNull(vm.SelectedVideoCodec);
        Assert.NotNull(vm.SelectedAudioCodec);
        Assert.NotNull(vm.SelectedPresetSpeed);
        Assert.NotNull(vm.SelectedFormat);
        Assert.NotEmpty(vm.Presets);
        Assert.NotEmpty(vm.Resolutions);
        Assert.NotEmpty(vm.VideoCodecs);
        Assert.NotEmpty(vm.AudioCodecs);
    }

    [Fact]
    public void Constructor_LoadsSettingsFromService()
    {
        // Arrange
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(m => m.Load()).Returns(new AppSettings());

        // Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);

        // Assert - verify settings service was consulted during initialization
        settingsService.Verify(s => s.Load(), Times.AtLeastOnce);

        // Verify ViewModel was properly initialized (details depend on preset/settings interaction)
        Assert.NotNull(vm.SelectedVideoCodec);
        Assert.NotNull(vm.SelectedFormat);
    }

    [Fact]
    public void Resolutions_ContainsExpectedOptions()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Contains(vm.Resolutions, r => r.Name == "Source");
        Assert.Contains(vm.Resolutions, r => r.Name.Contains("4K"));
        Assert.Contains(vm.Resolutions, r => r.Name.Contains("1080p"));
        Assert.Contains(vm.Resolutions, r => r.Name.Contains("720p"));
    }

    #endregion

    #region Export Mode Tests

    [Fact]
    public void IsDirectEncodeMode_WhenDirectEncode_ReturnsTrue()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            ExportMode = ExportMode.DirectEncode
        };

        // Assert
        Assert.True(vm.IsDirectEncodeMode);
        Assert.False(vm.IsTimelineWithEffectsMode);
    }

    [Fact]
    public void IsTimelineWithEffectsMode_WhenTimelineMode_ReturnsTrue()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            ExportMode = ExportMode.TimelineWithEffects
        };

        // Assert
        Assert.False(vm.IsDirectEncodeMode);
        Assert.True(vm.IsTimelineWithEffectsMode);
    }

    [Fact]
    public void IsDirectEncodeMode_Setter_ChangesExportMode()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            ExportMode = ExportMode.TimelineWithEffects
        };

        // Act
        vm.IsDirectEncodeMode = true;

        // Assert
        Assert.Equal(ExportMode.DirectEncode, vm.ExportMode);
    }

    [Fact]
    public void IsTimelineWithEffectsMode_Setter_ChangesExportMode()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            ExportMode = ExportMode.DirectEncode
        };

        // Act
        vm.IsTimelineWithEffectsMode = true;

        // Assert
        Assert.Equal(ExportMode.TimelineWithEffects, vm.ExportMode);
    }

    #endregion

    #region Codec Detection Tests

    [Theory]
    [InlineData("h264_nvenc", true)]
    [InlineData("hevc_nvenc", true)]
    [InlineData("libx264", false)]
    [InlineData("libx265", false)]
    public void IsNvencCodec_DetectsNvencCodecs(string codec, bool expected)
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            SelectedVideoCodec = codec
        };

        // Assert
        Assert.Equal(expected, vm.IsNvencCodec);
    }

    [Theory]
    [InlineData("prores_ks", true)]
    [InlineData("libx264", false)]
    [InlineData("h264_nvenc", false)]
    public void IsProresCodec_DetectsProresCodecs(string codec, bool expected)
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            SelectedVideoCodec = codec
        };

        // Assert
        Assert.Equal(expected, vm.IsProresCodec);
    }

    #endregion

    #region Queue Management Tests

    [Fact]
    public void AddToQueueCommand_WithValidPaths_AddsJobToQueue()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            InputPath = @"C:\test\input.mp4",
            OutputPath = @"C:\test\output.mp4"
        };

        // Act
        vm.AddToQueueCommand.Execute(null);

        // Assert
        Assert.Single(vm.ExportQueue);
        Assert.Equal("output.mp4", vm.ExportQueue[0].Name);
    }

    [Fact]
    public void AddToQueueCommand_WithEmptyInputPath_DoesNotAddJob()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            InputPath = "",
            OutputPath = @"C:\test\output.mp4"
        };

        // Act
        vm.AddToQueueCommand.Execute(null);

        // Assert
        Assert.Empty(vm.ExportQueue);
        Assert.Contains("Please select", vm.StatusText);
    }

    [Fact]
    public void RemoveJobCommand_RemovesJobFromQueue()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            InputPath = @"C:\test\input.mp4",
            OutputPath = @"C:\test\output.mp4"
        };
        vm.AddToQueueCommand.Execute(null);
        var job = vm.ExportQueue[0];

        // Act
        vm.RemoveJobCommand.Execute(job);

        // Assert
        Assert.Empty(vm.ExportQueue);
    }

    [Fact]
    public void ClearQueueCommand_RemovesCompletedJobs()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        var completedJob = new ExportJob { Name = "Completed", Status = ExportJobStatus.Completed };
        var pendingJob = new ExportJob { Name = "Pending", Status = ExportJobStatus.Pending };
        vm.ExportQueue.Add(completedJob);
        vm.ExportQueue.Add(pendingJob);

        // Act
        vm.ClearQueueCommand.Execute(null);

        // Assert
        Assert.Single(vm.ExportQueue);
        Assert.Equal("Pending", vm.ExportQueue[0].Name);
    }

    [Fact]
    public void ClearQueueCommand_RemovesFailedAndCancelledJobs()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        vm.ExportQueue.Add(new ExportJob { Name = "Failed", Status = ExportJobStatus.Failed });
        vm.ExportQueue.Add(new ExportJob { Name = "Cancelled", Status = ExportJobStatus.Cancelled });
        vm.ExportQueue.Add(new ExportJob { Name = "Pending", Status = ExportJobStatus.Pending });

        // Act
        vm.ClearQueueCommand.Execute(null);

        // Assert
        Assert.Single(vm.ExportQueue);
        Assert.Equal("Pending", vm.ExportQueue[0].Name);
    }

    #endregion

    #region Settings Persistence Tests

    [Fact]
    public void SelectedFormat_Change_SavesSettings()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);

        // Act
        vm.SelectedFormat = "mkv";

        // Assert
        settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.AtLeastOnce);
    }

    [Fact]
    public void SelectedVideoCodec_Change_SavesSettings()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);

        // Act
        vm.SelectedVideoCodec = "libx265";

        // Assert
        settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.AtLeastOnce);
    }

    [Fact]
    public void Quality_Change_SavesSettings()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);

        // Act
        vm.Quality = 18;

        // Assert
        settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.AtLeastOnce);
    }

    #endregion

    #region HasCurrentSource Tests

    [Fact]
    public void HasCurrentSource_WhenNoSource_ReturnsFalse()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.HasSource).Returns(false);
        var vm = new ExportViewModel(mediaPool.Object, CreateMockSettingsService().Object);

        // Assert
        Assert.False(vm.HasCurrentSource);
    }

    [Fact]
    public void HasCurrentSource_WhenHasSource_ReturnsTrue()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.HasSource).Returns(true);
        var vm = new ExportViewModel(mediaPool.Object, CreateMockSettingsService().Object);

        // Assert
        Assert.True(vm.HasCurrentSource);
    }

    [Fact]
    public void CurrentSourcePath_WhenHasSource_ReturnsPath()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var mediaItem = new MediaItem { FilePath = @"C:\videos\test.mp4" };
        mediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);
        var vm = new ExportViewModel(mediaPool.Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Equal(@"C:\videos\test.mp4", vm.CurrentSourcePath);
    }

    [Fact]
    public void CurrentSourcePath_WhenNoSource_ReturnsEmpty()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.CurrentSource).Returns((MediaItem?)null);
        var vm = new ExportViewModel(mediaPool.Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Equal("", vm.CurrentSourcePath);
    }

    #endregion

    #region Log Management Tests

    [Fact]
    public void ClearLogCommand_ClearsLogOutput()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            LogOutput = "Some log content"
        };

        // Act
        vm.ClearLogCommand.Execute(null);

        // Assert
        Assert.Equal("", vm.LogOutput);
    }

    #endregion

    #region Preset Selection Tests

    [Fact]
    public void SelectedPreset_WhenChanged_UpdatesCodecSettings()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        var preset = new ExportPreset
        {
            Name = "Test Preset",
            VideoCodec = "libx265",
            AudioCodec = "flac",
            Quality = 18,
            Preset = "slow",
            Format = "mkv"
        };

        // Act
        vm.SelectedPreset = preset;

        // Assert
        Assert.Equal("libx265", vm.SelectedVideoCodec);
        Assert.Equal("flac", vm.SelectedAudioCodec);
        Assert.Equal(18, vm.Quality);
        Assert.Equal("slow", vm.SelectedPresetSpeed);
        Assert.Equal("mkv", vm.SelectedFormat);
    }

    #endregion

    #region Status Tests

    [Fact]
    public void StatusText_InitialValue_IsReady()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Equal("Ready", vm.StatusText);
    }

    [Fact]
    public void IsEncoding_InitialValue_IsFalse()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.False(vm.IsEncoding);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Act & Assert - should not throw
        vm.Dispose();
        vm.Dispose();
    }

    #endregion
}
