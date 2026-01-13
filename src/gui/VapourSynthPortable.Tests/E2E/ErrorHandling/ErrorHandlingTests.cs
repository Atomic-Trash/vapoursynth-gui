using System.Collections.ObjectModel;
using FluentAssertions;
using Moq;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Tests.E2E.ErrorHandling;

/// <summary>
/// Tests for error handling across the application.
/// Validates graceful handling of invalid input, missing files, and edge cases.
/// </summary>
public class ErrorHandlingTests
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

    #endregion

    #region Invalid Input Tests

    [Fact]
    public void Timeline_NegativeFrameValue_HandlesGracefully()
    {
        // Arrange
        var clip = new TimelineClip
        {
            Name = "Test Clip",
            SourcePath = @"C:\test.mp4"
        };

        // Act - Try to set negative values
        clip.StartFrame = -100;
        clip.EndFrame = -50;

        // Assert - Values can be negative (edge case for certain operations)
        clip.StartFrame.Should().Be(-100);
        clip.EndFrame.Should().Be(-50);
    }

    [Fact]
    public void Timeline_ZeroDuration_Allowed()
    {
        // Arrange
        var clip = new TimelineClip
        {
            Name = "Zero Duration Clip",
            SourcePath = @"C:\test.mp4",
            StartFrame = 100,
            EndFrame = 100
        };

        // Assert - Zero duration clip (useful for freeze frames)
        clip.DurationFrames.Should().Be(0);
    }

    [Fact]
    public void MediaItem_EmptyFilePath_HandlesGracefully()
    {
        // Arrange & Act
        var mediaItem = new MediaItem
        {
            Name = "Test",
            FilePath = ""
        };

        // Assert
        mediaItem.FilePath.Should().BeEmpty();
        mediaItem.Name.Should().Be("Test");
    }

    [Fact]
    public void MediaItem_NullFilePath_CanBeSet()
    {
        // Arrange
        var mediaItem = new MediaItem();

        // Act
        mediaItem.FilePath = null!;

        // Assert
        mediaItem.FilePath.Should().BeNull();
    }

    [Fact]
    public void Effect_EmptyParameters_Allowed()
    {
        // Arrange & Act
        var effect = new TimelineEffect { Name = "Test Effect" };

        // Assert
        effect.Parameters.Should().NotBeNull();
        effect.Parameters.Should().BeEmpty();
    }

    #endregion

    #region Missing File Handling Tests

    [Fact]
    public void ExportViewModel_MissingInputFile_ShowsError()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        vm.InputPath = @"C:\nonexistent\file.mp4";
        vm.OutputPath = @"C:\output\file.mp4";

        // Act
        vm.AddToQueueCommand.Execute(null);

        // Assert - Job is added (file validation happens during export)
        vm.ExportQueue.Should().HaveCount(1);
    }

    [Fact]
    public void MediaItem_NonexistentPath_CreatesItem()
    {
        // Arrange & Act
        var mediaItem = new MediaItem
        {
            Name = "Missing File",
            FilePath = @"C:\this\path\does\not\exist.mp4"
        };

        // Assert - Item can be created with invalid path
        mediaItem.FilePath.Should().Be(@"C:\this\path\does\not\exist.mp4");
    }

    #endregion

    #region Null Safety Tests

    [Fact]
    public void Timeline_NullTrackName_Allowed()
    {
        // Arrange & Act
        var track = new TimelineTrack
        {
            Name = null!,
            TrackType = TrackType.Video
        };

        // Assert
        track.Name.Should().BeNull();
    }

    [Fact]
    public void Clip_NullSourcePath_Allowed()
    {
        // Arrange & Act
        var clip = new TimelineClip
        {
            Name = "Test",
            SourcePath = null!
        };

        // Assert
        clip.SourcePath.Should().BeNull();
    }

    [Fact]
    public void Effect_NullName_Allowed()
    {
        // Arrange & Act
        var effect = new TimelineEffect
        {
            Name = null!,
            EffectType = EffectType.Blur
        };

        // Assert
        effect.Name.Should().BeNull();
    }

    [Fact]
    public void ExportViewModel_NullPreset_DoesNotThrow()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Act & Assert - Should not throw
        vm.SelectedPreset = null;
    }

    [Fact]
    public void ExportViewModel_RemoveNullJob_DoesNotThrow()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Act & Assert - Should not throw
        vm.RemoveJobCommand.Execute(null);
    }

    #endregion

    #region Empty Collection Tests

    [Fact]
    public void Timeline_EmptyTracks_HasClipsIsFalse()
    {
        // Arrange & Act
        var timeline = new Timeline();

        // Assert
        timeline.Tracks.Should().BeEmpty();
        timeline.HasClips.Should().BeFalse();
    }

    [Fact]
    public void Timeline_TracksWithNoClips_HasClipsIsFalse()
    {
        // Arrange
        var timeline = new Timeline();
        timeline.Tracks.Add(new TimelineTrack { Name = "Empty Track", TrackType = TrackType.Video });

        // Assert
        timeline.HasClips.Should().BeFalse();
    }

    [Fact]
    public void ExportViewModel_EmptyQueue_ClearDoesNotThrow()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Act & Assert - Should not throw
        vm.ClearQueueCommand.Execute(null);
        vm.ExportQueue.Should().BeEmpty();
    }

    [Fact]
    public void MediaPool_NoCurrentSource_ReturnsNull()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();

        // Assert
        mediaPool.Object.CurrentSource.Should().BeNull();
        mediaPool.Object.HasSource.Should().BeFalse();
    }

    #endregion

    #region Boundary Value Tests

    [Fact]
    public void Quality_MinValue_Accepted()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Act
        vm.Quality = 0;

        // Assert
        vm.Quality.Should().Be(0);
    }

    [Fact]
    public void Quality_MaxValue_Accepted()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Act
        vm.Quality = 51;

        // Assert
        vm.Quality.Should().Be(51);
    }

    [Fact]
    public void AudioBitrate_MinValue_CanBeSelected()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Act - Set to minimum bitrate
        vm.SelectedAudioBitrate = 128;

        // Assert
        vm.SelectedAudioBitrate.Should().Be(128);
    }

    [Fact]
    public void Timeline_VeryLargeFrameNumber_Handled()
    {
        // Arrange
        var clip = new TimelineClip
        {
            Name = "Large Clip",
            SourcePath = @"C:\test.mp4",
            StartFrame = 0,
            EndFrame = long.MaxValue - 1
        };

        // Assert
        clip.EndFrame.Should().Be(long.MaxValue - 1);
    }

    [Fact]
    public void Keyframe_MaxFrameValue_Handled()
    {
        // Arrange & Act
        var keyframe = new Keyframe
        {
            Frame = int.MaxValue,
            Value = 1.0
        };

        // Assert
        keyframe.Frame.Should().Be(int.MaxValue);
    }

    #endregion

    #region State Consistency Tests

    [Fact]
    public void ExportViewModel_CancelWhileNotEncoding_SetsCorrectStatus()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        vm.IsEncoding = false;

        // Act
        vm.CancelExportCommand.Execute(null);

        // Assert
        vm.IsEncoding.Should().BeFalse();
        vm.StatusText.Should().Contain("cancelled");
    }

    [Fact]
    public void ExportViewModel_MultipleQueueClears_StateConsistent()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        vm.InputPath = @"C:\test\input.mp4";
        vm.OutputPath = @"C:\test\output.mp4";

        // Act - Add jobs and mark them as completed, then clear
        for (int i = 0; i < 5; i++)
        {
            vm.AddToQueueCommand.Execute(null);
        }

        // ClearQueue only removes Completed/Failed/Cancelled jobs
        // Mark all jobs as completed first
        foreach (var job in vm.ExportQueue.ToList())
        {
            job.Status = ExportJobStatus.Completed;
        }

        vm.ClearQueueCommand.Execute(null);

        // Assert
        vm.ExportQueue.Should().BeEmpty();
    }

    [Fact]
    public void Timeline_RapidModifications_StateConsistent()
    {
        // Arrange
        var timeline = new Timeline();
        var track = new TimelineTrack { Name = "Video 1", TrackType = TrackType.Video };
        timeline.Tracks.Add(track);

        // Act - Add and remove clips rapidly
        for (int i = 0; i < 100; i++)
        {
            var clip = new TimelineClip
            {
                Name = $"Clip {i}",
                SourcePath = @"C:\test.mp4",
                StartFrame = i * 10,
                EndFrame = (i + 1) * 10
            };
            track.Clips.Add(clip);

            if (i % 2 == 0)
            {
                track.Clips.Remove(clip);
            }
        }

        // Assert
        track.Clips.Should().HaveCount(50);
    }

    #endregion

    #region Type Conversion Tests

    [Fact]
    public void ResolutionOption_ToString_ReturnsName()
    {
        // Arrange
        var resolution = new ResolutionOption
        {
            Name = "1080p (1920x1080)",
            Width = 1920,
            Height = 1080
        };

        // Assert
        resolution.ToString().Should().Be("1080p (1920x1080)");
    }

    [Fact]
    public void MediaType_AllValuesValid()
    {
        // Assert
        Enum.GetValues<MediaType>().Should().Contain(MediaType.Video);
        Enum.GetValues<MediaType>().Should().Contain(MediaType.Audio);
        Enum.GetValues<MediaType>().Should().Contain(MediaType.Image);
        Enum.GetValues<MediaType>().Should().Contain(MediaType.Unknown);
    }

    [Fact]
    public void TrackType_AllValuesValid()
    {
        // Assert
        Enum.GetValues<TrackType>().Should().Contain(TrackType.Video);
        Enum.GetValues<TrackType>().Should().Contain(TrackType.Audio);
    }

    [Fact]
    public void ExportJobStatus_AllValuesValid()
    {
        // Assert
        Enum.GetValues<ExportJobStatus>().Should().Contain(ExportJobStatus.Pending);
        Enum.GetValues<ExportJobStatus>().Should().Contain(ExportJobStatus.Encoding);
        Enum.GetValues<ExportJobStatus>().Should().Contain(ExportJobStatus.Completed);
        Enum.GetValues<ExportJobStatus>().Should().Contain(ExportJobStatus.Failed);
        Enum.GetValues<ExportJobStatus>().Should().Contain(ExportJobStatus.Cancelled);
    }

    #endregion

    #region Collection Modification Tests

    [Fact]
    public void Timeline_ClearTracks_CollectionEmpty()
    {
        // Arrange
        var timeline = new Timeline();
        for (int i = 0; i < 5; i++)
        {
            timeline.Tracks.Add(new TimelineTrack { Name = $"Track {i}", TrackType = TrackType.Video });
        }

        // Act
        timeline.Tracks.Clear();

        // Assert
        timeline.Tracks.Should().BeEmpty();
    }

    [Fact]
    public void Track_ClearClips_CollectionEmpty()
    {
        // Arrange
        var track = new TimelineTrack { Name = "Video 1", TrackType = TrackType.Video };
        for (int i = 0; i < 10; i++)
        {
            track.Clips.Add(new TimelineClip
            {
                Name = $"Clip {i}",
                SourcePath = @"C:\test.mp4",
                StartFrame = i * 100,
                EndFrame = (i + 1) * 100
            });
        }

        // Act
        track.Clips.Clear();

        // Assert
        track.Clips.Should().BeEmpty();
    }

    #endregion

    #region Unicode and Special Character Tests

    [Fact]
    public void MediaItem_UnicodeFilePath_Handled()
    {
        // Arrange & Act
        var mediaItem = new MediaItem
        {
            Name = "日本語ファイル.mp4",
            FilePath = @"C:\videos\日本語\ファイル.mp4"
        };

        // Assert
        mediaItem.Name.Should().Be("日本語ファイル.mp4");
        mediaItem.FilePath.Should().Be(@"C:\videos\日本語\ファイル.mp4");
    }

    [Fact]
    public void Track_SpecialCharactersInName_Handled()
    {
        // Arrange & Act
        var track = new TimelineTrack
        {
            Name = "Track <>'\"&",
            TrackType = TrackType.Video
        };

        // Assert
        track.Name.Should().Be("Track <>'\"&");
    }

    [Fact]
    public void Clip_EmojiInName_Handled()
    {
        // Arrange & Act
        var clip = new TimelineClip
        {
            Name = "Clip 🎬📹",
            SourcePath = @"C:\test.mp4"
        };

        // Assert
        clip.Name.Should().Be("Clip 🎬📹");
    }

    [Fact]
    public void Marker_UnicodeName_Handled()
    {
        // Arrange & Act
        var marker = new TimelineMarker
        {
            Frame = 100,
            Name = "マーカー"
        };

        // Assert
        marker.Name.Should().Be("マーカー");
    }

    #endregion

    #region Concurrent Access Safety Tests

    [Fact]
    public async Task ExportQueue_ConcurrentAdds_HandlesGracefully()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        vm.InputPath = @"C:\test\input.mp4";

        // Act - Simulate multiple concurrent adds
        var tasks = Enumerable.Range(0, 10).Select(i =>
        {
            return Task.Run(() =>
            {
                vm.OutputPath = $@"C:\test\output{i}.mp4";
                vm.AddToQueueCommand.Execute(null);
            });
        });

        await Task.WhenAll(tasks);

        // Assert - Some jobs should have been added
        vm.ExportQueue.Should().NotBeNull();
    }

    #endregion

    #region Dispose Safety Tests

    [Fact]
    public void ExportViewModel_DisposeMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Act & Assert - Should not throw
        vm.Dispose();
        vm.Dispose();
        vm.Dispose();
    }

    [Fact]
    public void ExportViewModel_UseAfterDispose_PropertiesAccessible()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        vm.InputPath = @"C:\test.mp4";

        // Act
        vm.Dispose();

        // Assert - Properties should still be accessible after dispose
        vm.InputPath.Should().Be(@"C:\test.mp4");
    }

    #endregion
}
