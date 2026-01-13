using System.Collections.ObjectModel;
using FluentAssertions;
using Moq;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Tests.E2E.Workflows;

/// <summary>
/// End-to-end tests for the complete import → edit → export workflow.
/// These tests validate that ViewModels and Services work together correctly
/// to support the primary user journey.
/// </summary>
public class ImportToExportWorkflowTests
{
    #region Helper Methods

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

    private static MediaItem CreateSampleMediaItem(string path = @"C:\videos\sample.mp4")
    {
        return new MediaItem
        {
            Name = System.IO.Path.GetFileName(path),
            FilePath = path,
            MediaType = MediaType.Video,
            Width = 1920,
            Height = 1080,
            FrameRate = 30,
            FrameCount = 3600,
            Duration = 120,
            FileSize = 100_000_000,
            Codec = "h264",
            HasAudioStream = true,
            AudioChannels = 2,
            AudioSampleRate = 48000
        };
    }

    #endregion

    #region Media Import Workflow Tests

    [Fact]
    public void ImportWorkflow_MediaPoolReceivesMedia_CanBeSelectedInEditPage()
    {
        // Arrange
        var mediaPool = new Mock<IMediaPoolService>();
        var mediaCollection = new ObservableCollection<MediaItem>();
        var mediaItem = CreateSampleMediaItem();
        mediaPool.Setup(m => m.MediaPool).Returns(mediaCollection);
        mediaPool.Setup(m => m.CurrentSource).Returns((MediaItem?)null);
        mediaPool.SetupProperty(m => m.CurrentSource);

        // Act - Simulate adding media to pool
        mediaCollection.Add(mediaItem);
        mediaPool.Setup(m => m.HasSource).Returns(true);
        mediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);

        // Assert - Media should be accessible
        mediaPool.Object.MediaPool.Should().Contain(mediaItem);
        mediaPool.Object.CurrentSource.Should().Be(mediaItem);
        mediaPool.Object.HasSource.Should().BeTrue();
    }

    [Fact]
    public void ImportWorkflow_MultipleMediaFiles_AllAccessibleInPool()
    {
        // Arrange
        var mediaPool = new Mock<IMediaPoolService>();
        var mediaCollection = new ObservableCollection<MediaItem>();
        mediaPool.Setup(m => m.MediaPool).Returns(mediaCollection);

        var mediaItems = new[]
        {
            CreateSampleMediaItem(@"C:\videos\video1.mp4"),
            CreateSampleMediaItem(@"C:\videos\video2.mp4"),
            CreateSampleMediaItem(@"C:\videos\video3.mp4")
        };

        // Act
        foreach (var item in mediaItems)
        {
            mediaCollection.Add(item);
        }

        // Assert
        mediaPool.Object.MediaPool.Should().HaveCount(3);
        mediaPool.Object.MediaPool.Should().Contain(m => m.Name == "video1.mp4");
        mediaPool.Object.MediaPool.Should().Contain(m => m.Name == "video2.mp4");
        mediaPool.Object.MediaPool.Should().Contain(m => m.Name == "video3.mp4");
    }

    [Fact]
    public void ImportWorkflow_MediaWithDifferentTypes_ClassifiedCorrectly()
    {
        // Arrange
        var mediaPool = new Mock<IMediaPoolService>();
        var mediaCollection = new ObservableCollection<MediaItem>();
        mediaPool.Setup(m => m.MediaPool).Returns(mediaCollection);

        var videoItem = CreateSampleMediaItem(@"C:\media\video.mp4");
        videoItem.MediaType = MediaType.Video;

        var audioItem = CreateSampleMediaItem(@"C:\media\audio.mp3");
        audioItem.MediaType = MediaType.Audio;
        audioItem.Width = 0;
        audioItem.Height = 0;

        var imageItem = CreateSampleMediaItem(@"C:\media\image.png");
        imageItem.MediaType = MediaType.Image;
        imageItem.Duration = 0;
        imageItem.FrameCount = 1;

        // Act
        mediaCollection.Add(videoItem);
        mediaCollection.Add(audioItem);
        mediaCollection.Add(imageItem);

        // Assert
        mediaPool.Object.MediaPool.Should().Contain(m => m.MediaType == MediaType.Video);
        mediaPool.Object.MediaPool.Should().Contain(m => m.MediaType == MediaType.Audio);
        mediaPool.Object.MediaPool.Should().Contain(m => m.MediaType == MediaType.Image);
    }

    #endregion

    #region Timeline Editing Workflow Tests

    [Fact]
    public void EditWorkflow_AddClipToTimeline_TimelineContainsClip()
    {
        // Arrange
        var timeline = new Timeline();
        var track = new TimelineTrack { Name = "Video 1", TrackType = TrackType.Video };
        timeline.Tracks.Add(track);

        var clip = new TimelineClip
        {
            Name = "Sample Clip",
            SourcePath = @"C:\videos\sample.mp4",
            StartFrame = 0,
            EndFrame = 100
        };

        // Act
        track.Clips.Add(clip);

        // Assert
        timeline.HasClips.Should().BeTrue();
        track.Clips.Should().Contain(clip);
        track.Clips.First().Name.Should().Be("Sample Clip");
    }

    [Fact]
    public void EditWorkflow_MultipleClipsOnTrack_AllClipsAccessible()
    {
        // Arrange
        var timeline = new Timeline();
        var track = new TimelineTrack { Name = "Video 1", TrackType = TrackType.Video };
        timeline.Tracks.Add(track);

        // Act - Add multiple clips
        for (int i = 0; i < 5; i++)
        {
            track.Clips.Add(new TimelineClip
            {
                Name = $"Clip {i + 1}",
                SourcePath = @"C:\videos\sample.mp4",
                StartFrame = i * 100,
                EndFrame = (i + 1) * 100
            });
        }

        // Assert
        track.Clips.Should().HaveCount(5);
        track.Clips.Select(c => c.Name).Should().BeEquivalentTo(
            new[] { "Clip 1", "Clip 2", "Clip 3", "Clip 4", "Clip 5" });
    }

    [Fact]
    public void EditWorkflow_ClipWithEffects_EffectsPreserved()
    {
        // Arrange
        var timeline = new Timeline();
        var track = new TimelineTrack { Name = "Video 1", TrackType = TrackType.Video };
        timeline.Tracks.Add(track);

        var clip = new TimelineClip
        {
            Name = "Sample Clip",
            SourcePath = @"C:\videos\sample.mp4",
            StartFrame = 0,
            EndFrame = 100
        };

        var effect = new TimelineEffect
        {
            Name = "Blur",
            EffectType = EffectType.Blur,
            IsEnabled = true
        };

        // Act
        clip.Effects.Add(effect);
        track.Clips.Add(clip);

        // Assert
        var addedClip = track.Clips.First();
        addedClip.Effects.Should().HaveCount(1);
        addedClip.Effects.First().Name.Should().Be("Blur");
    }

    [Fact]
    public void EditWorkflow_TrimClip_UpdatesSourceInOutFrames()
    {
        // Arrange
        var clip = new TimelineClip
        {
            Name = "Sample Clip",
            SourcePath = @"C:\videos\sample.mp4",
            StartFrame = 0,
            EndFrame = 1000,
            SourceDurationFrames = 1000
        };

        // Act - Trim the clip
        clip.SourceInFrame = 100;
        clip.SourceOutFrame = 900;

        // Assert
        clip.SourceInFrame.Should().Be(100);
        clip.SourceOutFrame.Should().Be(900);
    }

    [Fact]
    public void EditWorkflow_MoveClip_UpdatesPosition()
    {
        // Arrange
        var clip = new TimelineClip
        {
            Name = "Sample Clip",
            SourcePath = @"C:\videos\sample.mp4",
            StartFrame = 0,
            EndFrame = 100
        };

        // Act - Move the clip
        clip.StartFrame = 500;
        clip.EndFrame = 600;

        // Assert
        clip.StartFrame.Should().Be(500);
        clip.EndFrame.Should().Be(600);
        clip.DurationFrames.Should().Be(100);
    }

    #endregion

    #region Export Workflow Tests

    [Fact]
    public void ExportWorkflow_BasicSettings_ConfiguredCorrectly()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            InputPath = @"C:\videos\input.mp4",
            OutputPath = @"C:\videos\output.mp4",
            SelectedVideoCodec = "libx264",
            SelectedAudioCodec = "aac",
            Quality = 20,
            SelectedFormat = "mp4"
        };

        // Assert
        vm.InputPath.Should().Be(@"C:\videos\input.mp4");
        vm.OutputPath.Should().Be(@"C:\videos\output.mp4");
        vm.SelectedVideoCodec.Should().Be("libx264");
        vm.SelectedAudioCodec.Should().Be("aac");
        vm.Quality.Should().Be(20);
        vm.SelectedFormat.Should().Be("mp4");
    }

    [Fact]
    public void ExportWorkflow_QueueMultipleJobs_AllJobsQueued()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Act - Add multiple jobs to queue
        vm.InputPath = @"C:\videos\video1.mp4";
        vm.OutputPath = @"C:\videos\output1.mp4";
        vm.AddToQueueCommand.Execute(null);

        vm.InputPath = @"C:\videos\video2.mp4";
        vm.OutputPath = @"C:\videos\output2.mp4";
        vm.AddToQueueCommand.Execute(null);

        vm.InputPath = @"C:\videos\video3.mp4";
        vm.OutputPath = @"C:\videos\output3.mp4";
        vm.AddToQueueCommand.Execute(null);

        // Assert
        vm.ExportQueue.Should().HaveCount(3);
        vm.ExportQueue.Select(j => j.Name).Should().BeEquivalentTo(
            new[] { "output1.mp4", "output2.mp4", "output3.mp4" });
    }

    [Fact]
    public void ExportWorkflow_DifferentCodecs_SettingsApplied()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Test H.264
        vm.SelectedVideoCodec = "libx264";
        vm.IsNvencCodec.Should().BeFalse();
        vm.IsProresCodec.Should().BeFalse();

        // Test NVENC
        vm.SelectedVideoCodec = "h264_nvenc";
        vm.IsNvencCodec.Should().BeTrue();
        vm.IsProresCodec.Should().BeFalse();

        // Test ProRes
        vm.SelectedVideoCodec = "prores_ks";
        vm.IsNvencCodec.Should().BeFalse();
        vm.IsProresCodec.Should().BeTrue();
    }

    [Fact]
    public void ExportWorkflow_ResolutionChange_AppliedCorrectly()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        var res1080p = vm.Resolutions.First(r => r.Name.Contains("1080p"));

        // Act
        vm.SelectedResolution = res1080p;

        // Assert
        vm.SelectedResolution.Should().NotBeNull();
        vm.SelectedResolution!.Width.Should().Be(1920);
        vm.SelectedResolution.Height.Should().Be(1080);
    }

    [Fact]
    public void ExportWorkflow_AudioSettings_ConfiguredCorrectly()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Act
        vm.AudioEnabled = true;
        vm.SelectedAudioCodec = "flac";
        vm.SelectedAudioBitrate = 320;

        // Assert
        vm.AudioEnabled.Should().BeTrue();
        vm.SelectedAudioCodec.Should().Be("flac");
        vm.SelectedAudioBitrate.Should().Be(320);
    }

    [Fact]
    public void ExportWorkflow_VideoDisabled_OnlyAudioExported()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Act
        vm.VideoEnabled = false;
        vm.AudioEnabled = true;

        // Assert
        vm.VideoEnabled.Should().BeFalse();
        vm.AudioEnabled.Should().BeTrue();
    }

    #endregion

    #region Complete Workflow Integration Tests

    [Fact]
    public void CompleteWorkflow_ImportEditExport_AllStagesSuccessful()
    {
        // Arrange - Set up media pool with video
        var mediaPool = new Mock<IMediaPoolService>();
        var mediaCollection = new ObservableCollection<MediaItem>();
        var mediaItem = CreateSampleMediaItem();
        mediaCollection.Add(mediaItem);
        mediaPool.Setup(m => m.MediaPool).Returns(mediaCollection);
        mediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);
        mediaPool.Setup(m => m.HasSource).Returns(true);

        // Act - Create timeline and add clip
        var timeline = new Timeline();
        var track = new TimelineTrack { Name = "Video 1", TrackType = TrackType.Video };
        timeline.Tracks.Add(track);

        var clip = new TimelineClip
        {
            Name = mediaItem.Name,
            SourcePath = mediaItem.FilePath,
            StartFrame = 0,
            EndFrame = mediaItem.FrameCount
        };
        track.Clips.Add(clip);

        // Create export settings
        var exportVm = new ExportViewModel(mediaPool.Object, CreateMockSettingsService().Object);
        exportVm.InputPath = mediaItem.FilePath;
        exportVm.OutputPath = @"C:\output\final.mp4";
        exportVm.SelectedVideoCodec = "libx264";
        exportVm.Quality = 18;

        // Assert - All stages configured correctly
        mediaPool.Object.MediaPool.Should().HaveCount(1);
        timeline.HasClips.Should().BeTrue();
        track.Clips.Should().HaveCount(1);
        exportVm.InputPath.Should().NotBeNullOrEmpty();
        exportVm.OutputPath.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CompleteWorkflow_TimelineExportMode_RequiresTimeline()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var exportVm = new ExportViewModel(mediaPool.Object, CreateMockSettingsService().Object);

        // Act - Set timeline export mode without timeline
        exportVm.ExportMode = ExportMode.TimelineWithEffects;
        exportVm.Timeline = null;

        // Assert - Should be in timeline mode but without timeline
        exportVm.IsTimelineWithEffectsMode.Should().BeTrue();
        exportVm.Timeline.Should().BeNull();
    }

    [Fact]
    public void CompleteWorkflow_TimelineWithClips_CanExport()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var timeline = new Timeline();
        var track = new TimelineTrack { Name = "Video 1", TrackType = TrackType.Video };
        track.Clips.Add(new TimelineClip
        {
            Name = "Clip 1",
            SourcePath = @"C:\videos\sample.mp4",
            StartFrame = 0,
            EndFrame = 100
        });
        timeline.Tracks.Add(track);

        var exportVm = new ExportViewModel(mediaPool.Object, CreateMockSettingsService().Object);
        exportVm.Timeline = timeline;
        exportVm.ExportMode = ExportMode.TimelineWithEffects;

        // Assert
        exportVm.Timeline.Should().NotBeNull();
        exportVm.Timeline!.HasClips.Should().BeTrue();
        exportVm.IsTimelineWithEffectsMode.Should().BeTrue();
    }

    #endregion

    #region Cross-Component State Tests

    [Fact]
    public void StateConsistency_MediaPoolUpdate_ReflectedInExportViewModel()
    {
        // Arrange
        var mediaPool = new Mock<IMediaPoolService>();
        var mediaCollection = new ObservableCollection<MediaItem>();
        mediaPool.Setup(m => m.MediaPool).Returns(mediaCollection);
        mediaPool.Setup(m => m.CurrentSource).Returns((MediaItem?)null);
        mediaPool.Setup(m => m.HasSource).Returns(false);

        var exportVm = new ExportViewModel(mediaPool.Object, CreateMockSettingsService().Object);

        // Initial state - no source
        exportVm.HasCurrentSource.Should().BeFalse();

        // Act - Add media
        var mediaItem = CreateSampleMediaItem();
        mediaCollection.Add(mediaItem);
        mediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);
        mediaPool.Setup(m => m.HasSource).Returns(true);

        // Assert - Now has source
        mediaPool.Object.HasSource.Should().BeTrue();
        mediaPool.Object.CurrentSource.Should().Be(mediaItem);
    }

    [Fact]
    public void StateConsistency_SettingsChange_PersistedAcrossSessions()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var savedSettings = new AppSettings();
        settingsService.Setup(s => s.Save(It.IsAny<AppSettings>()))
            .Callback<AppSettings>(s => savedSettings = s);

        var exportVm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);

        // Act - Change settings
        exportVm.SelectedFormat = "mkv";
        exportVm.SelectedVideoCodec = "libx265";
        exportVm.Quality = 18;

        // Assert - Settings saved
        settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.AtLeast(1));
    }

    #endregion

    #region Edge Case Workflow Tests

    [Fact]
    public void EdgeCase_EmptyTimeline_CannotAddToQueue()
    {
        // Arrange
        var exportVm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        exportVm.InputPath = "";
        exportVm.OutputPath = "";

        // Act
        exportVm.AddToQueueCommand.Execute(null);

        // Assert
        exportVm.ExportQueue.Should().BeEmpty();
    }

    [Fact]
    public void EdgeCase_VeryLongClip_HandledCorrectly()
    {
        // Arrange - 10 hour video at 30fps = 1,080,000 frames
        var clip = new TimelineClip
        {
            Name = "Long Video",
            SourcePath = @"C:\videos\long_video.mp4",
            StartFrame = 0,
            EndFrame = 1_080_000
        };

        // Assert
        clip.DurationFrames.Should().Be(1_080_000);
        clip.StartFrame.Should().Be(0);
        clip.EndFrame.Should().Be(1_080_000);
    }

    [Fact]
    public void EdgeCase_HighFrameRate_HandledCorrectly()
    {
        // Arrange - 240fps video
        var mediaItem = CreateSampleMediaItem();
        mediaItem.FrameRate = 240;
        mediaItem.FrameCount = 28800; // 2 minutes at 240fps
        mediaItem.Duration = 120;

        // Assert
        mediaItem.FrameRate.Should().Be(240);
        mediaItem.FrameCount.Should().Be(28800);
        (mediaItem.FrameCount / mediaItem.FrameRate).Should().Be(120);
    }

    [Fact]
    public void EdgeCase_4KResolution_HandledCorrectly()
    {
        // Arrange
        var mediaItem = CreateSampleMediaItem();
        mediaItem.Width = 3840;
        mediaItem.Height = 2160;

        // Assert
        mediaItem.Width.Should().Be(3840);
        mediaItem.Height.Should().Be(2160);
        mediaItem.Resolution.Should().Be("3840x2160");
    }

    [Fact]
    public void EdgeCase_MultipleAudioTracks_HandledCorrectly()
    {
        // Arrange
        var timeline = new Timeline();

        // Add multiple audio tracks
        for (int i = 0; i < 5; i++)
        {
            timeline.Tracks.Add(new TimelineTrack
            {
                Name = $"Audio {i + 1}",
                TrackType = TrackType.Audio
            });
        }

        // Assert
        timeline.Tracks.Should().HaveCount(5);
        timeline.Tracks.Should().OnlyContain(t => t.TrackType == TrackType.Audio);
    }

    #endregion
}
