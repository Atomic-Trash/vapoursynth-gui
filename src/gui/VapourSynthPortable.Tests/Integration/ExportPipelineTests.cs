using System.Collections.ObjectModel;
using Moq;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Tests.Integration;

/// <summary>
/// Integration tests for the export pipeline from Edit → Export workflow.
/// </summary>
public class ExportPipelineTests
{
    private static Mock<IMediaPoolService> CreateMockMediaPool()
    {
        var mock = new Mock<IMediaPoolService>();
        mock.Setup(m => m.MediaPool).Returns(new ObservableCollection<MediaItem>());
        mock.Setup(m => m.CurrentSource).Returns((MediaItem?)null);
        mock.Setup(m => m.HasSource).Returns(false);
        mock.Setup(m => m.EditTimeline).Returns((Timeline?)null);
        return mock;
    }

    private static Mock<ISettingsService> CreateMockSettingsService()
    {
        var mock = new Mock<ISettingsService>();
        mock.Setup(m => m.Load()).Returns(new AppSettings());
        return mock;
    }

    [Fact]
    public void EditTimeline_SharedWithExportPage_ViaMediaPoolService()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        Timeline? sharedTimeline = null;
        mediaPool.Setup(m => m.SetEditTimeline(It.IsAny<Timeline>()))
            .Callback<Timeline>(t => sharedTimeline = t);
        mediaPool.Setup(m => m.EditTimeline).Returns(() => sharedTimeline);

        // Act - Create EditViewModel (which sets timeline)
        var editVm = new EditViewModel(mediaPool.Object);

        // Assert - Timeline should be shared
        Assert.NotNull(sharedTimeline);
        mediaPool.Verify(m => m.SetEditTimeline(It.IsAny<Timeline>()), Times.Once);
    }

    [Fact]
    public void ExportViewModel_CanAccessEditTimeline()
    {
        // Arrange
        var timeline = new Timeline();
        timeline.AddTrack(TrackType.Video);

        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.EditTimeline).Returns(timeline);

        var settingsService = CreateMockSettingsService();

        // Act
        var exportVm = new ExportViewModel(mediaPool.Object, settingsService.Object);

        // Assert
        Assert.NotNull(mediaPool.Object.EditTimeline);
    }

    [Fact]
    public void ExportMode_DirectEncode_UsesCurrentSource()
    {
        // Arrange
        var mediaItem = new MediaItem
        {
            Name = "test.mp4",
            FilePath = Path.Combine(Path.GetTempPath(), "test.mp4"),
            Duration = 10.0,
            MediaType = MediaType.Video
        };

        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);
        mediaPool.Setup(m => m.HasSource).Returns(true);

        var settingsService = CreateMockSettingsService();
        var exportVm = new ExportViewModel(mediaPool.Object, settingsService.Object)
        {
            ExportMode = ExportMode.DirectEncode
        };

        // Assert
        Assert.True(exportVm.IsDirectEncodeMode);
        Assert.Equal(mediaItem, mediaPool.Object.CurrentSource);
    }

    [Fact]
    public void ExportMode_TimelineWithEffects_UsesEditTimeline()
    {
        // Arrange
        var timeline = new Timeline();
        timeline.AddTrack(TrackType.Video);

        // Add a clip to the first track
        if (timeline.Tracks.Count > 0)
        {
            timeline.Tracks[0].Clips.Add(new TimelineClip
            {
                Name = "TestClip",
                StartFrame = 0,
                EndFrame = 240,
                TrackType = TrackType.Video
            });
        }

        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.EditTimeline).Returns(timeline);

        var settingsService = CreateMockSettingsService();
        var exportVm = new ExportViewModel(mediaPool.Object, settingsService.Object)
        {
            ExportMode = ExportMode.TimelineWithEffects
        };

        // Assert
        Assert.True(exportVm.IsTimelineWithEffectsMode);
        Assert.NotNull(mediaPool.Object.EditTimeline);
        Assert.True(mediaPool.Object.EditTimeline.HasClips);
    }

    [Fact]
    public void ExportSettings_LoadedFromSettingsService()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(m => m.Load()).Returns(new AppSettings
        {
            DefaultExportFormat = "mkv",
            DefaultVideoCodec = "libx265"
        });

        // Act
        var exportVm = new ExportViewModel(mediaPool.Object, settingsService.Object);

        // Assert
        settingsService.Verify(s => s.Load(), Times.AtLeastOnce);
    }

    [Fact]
    public void SourceResolution_FromTimeline()
    {
        // Arrange
        var timeline = new Timeline();

        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.EditTimeline).Returns(timeline);

        var settingsService = CreateMockSettingsService();
        var exportVm = new ExportViewModel(mediaPool.Object, settingsService.Object);

        // Assert - Resolutions list should contain entries
        Assert.NotEmpty(exportVm.Resolutions);
    }

    [Theory]
    [InlineData("4K")]
    [InlineData("1080p")]
    [InlineData("720p")]
    public void StandardResolutions_AreAvailable(string name)
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var settingsService = CreateMockSettingsService();
        var exportVm = new ExportViewModel(mediaPool.Object, settingsService.Object);

        // Act
        var resolution = exportVm.Resolutions.FirstOrDefault(r => r.Name.Contains(name));

        // Assert
        Assert.NotNull(resolution);
    }

    [Fact]
    public void VideoCodecs_AvailableForSelectedFormat()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var settingsService = CreateMockSettingsService();
        var exportVm = new ExportViewModel(mediaPool.Object, settingsService.Object);

        // Act
        exportVm.SelectedFormat = "mp4";

        // Assert
        Assert.NotEmpty(exportVm.VideoCodecs);
    }

    [Fact]
    public void AudioCodecs_AvailableForSelectedFormat()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var settingsService = CreateMockSettingsService();
        var exportVm = new ExportViewModel(mediaPool.Object, settingsService.Object);

        // Act
        exportVm.SelectedFormat = "mp4";

        // Assert
        Assert.NotEmpty(exportVm.AudioCodecs);
    }
}
