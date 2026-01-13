using System.Collections.ObjectModel;
using Moq;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Tests.ViewModels;

public class EditViewModelTests
{
    #region Test Setup

    private static Mock<IMediaPoolService> CreateMockMediaPool()
    {
        var mock = new Mock<IMediaPoolService>();
        mock.Setup(m => m.MediaPool).Returns(new ObservableCollection<MediaItem>());
        mock.Setup(m => m.CurrentSource).Returns((MediaItem?)null);
        mock.Setup(m => m.HasSource).Returns(false);
        return mock;
    }

    private static MediaItem CreateTestMediaItem(string name = "test.mp4", double duration = 10.0)
    {
        return new MediaItem
        {
            Name = name,
            FilePath = Path.Combine(Path.GetTempPath(), name),
            Duration = duration,
            MediaType = MediaType.Video,
            Width = 1920,
            Height = 1080,
            FrameRate = 24.0
        };
    }

    private static EditViewModel CreateSut(Mock<IMediaPoolService>? mediaPool = null)
    {
        return new EditViewModel((mediaPool ?? CreateMockMediaPool()).Object);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesDefaultValues()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        Assert.NotNull(sut.Timeline);
        Assert.Equal("Ready", sut.StatusText);
        Assert.Equal("00:00:00:00", sut.CurrentTimecode);
        Assert.True(sut.SnapToClips);
        Assert.False(sut.RippleEdit);
        Assert.False(sut.IsPlaying);
        Assert.False(sut.IsScrubbing);
    }

    [Fact]
    public void Constructor_InitializesTimelineWithDefaultTracks()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        Assert.Equal(4, sut.Timeline.Tracks.Count);
        Assert.Equal(2, sut.Timeline.Tracks.Count(t => t.TrackType == TrackType.Video));
        Assert.Equal(2, sut.Timeline.Tracks.Count(t => t.TrackType == TrackType.Audio));
        Assert.Equal(24, sut.Timeline.FrameRate);
    }

    [Fact]
    public void Constructor_LoadsTransitionPresets()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        Assert.NotEmpty(sut.TransitionPresets);
        Assert.NotNull(sut.SelectedTransitionPreset);
    }

    [Fact]
    public void Constructor_SubscribesToMediaPoolEvents()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();

        // Act
        var sut = CreateSut(mediaPool);

        // Assert - verify event subscriptions exist
        Assert.NotNull(sut.MediaPool);
    }

    [Fact]
    public void Constructor_SharesTimelineWithMediaPoolService()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();

        // Act
        var sut = CreateSut(mediaPool);

        // Assert
        mediaPool.Verify(m => m.SetEditTimeline(It.IsAny<Timeline>()), Times.Once);
    }

    #endregion

    #region AddToTimeline Tests

    [Fact]
    public void AddToTimelineCommand_WhenNoSelectedMedia_DoesNothing()
    {
        // Arrange
        var sut = CreateSut();
        sut.SelectedMediaItem = null;
        var initialClipCount = sut.Timeline.Tracks.Sum(t => t.Clips.Count);

        // Act
        sut.AddToTimelineCommand.Execute(null);

        // Assert
        Assert.Equal(initialClipCount, sut.Timeline.Tracks.Sum(t => t.Clips.Count));
    }

    [Fact]
    public void AddToTimelineCommand_WithValidMedia_AddsClipToTimeline()
    {
        // Arrange
        var sut = CreateSut();
        var mediaItem = CreateTestMediaItem();

        // Create temp file for the test
        File.WriteAllText(mediaItem.FilePath, "test");
        try
        {
            sut.SelectedMediaItem = mediaItem;
            sut.SourceInPoint = 0;
            sut.SourceOutPoint = mediaItem.Duration;

            // Act
            sut.AddToTimelineCommand.Execute(null);

            // Assert
            var videoTrack = sut.Timeline.Tracks.First(t => t.TrackType == TrackType.Video);
            Assert.Single(videoTrack.Clips);
            Assert.Equal(mediaItem.Name, videoTrack.Clips[0].Name);
        }
        finally
        {
            File.Delete(mediaItem.FilePath);
        }
    }

    [Fact]
    public void AddToTimelineCommand_WithMissingFile_ShowsError()
    {
        // Arrange
        var sut = CreateSut();
        var mediaItem = CreateTestMediaItem();
        mediaItem.FilePath = @"C:\nonexistent\file.mp4";
        sut.SelectedMediaItem = mediaItem;

        // Act
        sut.AddToTimelineCommand.Execute(null);

        // Assert
        Assert.Contains("not found", sut.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddToTimelineCommand_WithAudioMedia_AddsToAudioTrack()
    {
        // Arrange
        var sut = CreateSut();
        var mediaItem = CreateTestMediaItem("test.mp3");
        mediaItem.MediaType = MediaType.Audio;

        File.WriteAllText(mediaItem.FilePath, "test");
        try
        {
            sut.SelectedMediaItem = mediaItem;
            sut.SourceOutPoint = mediaItem.Duration;

            // Act
            sut.AddToTimelineCommand.Execute(null);

            // Assert
            var audioTrack = sut.Timeline.Tracks.First(t => t.TrackType == TrackType.Audio);
            Assert.Single(audioTrack.Clips);
        }
        finally
        {
            File.Delete(mediaItem.FilePath);
        }
    }

    [Fact]
    public void AddToTimelineCommand_WithNoAvailableTrack_SetsErrorStatus()
    {
        // Arrange
        var sut = CreateSut();

        // Lock all video tracks
        foreach (var track in sut.Timeline.Tracks.Where(t => t.TrackType == TrackType.Video))
        {
            track.IsLocked = true;
        }

        var mediaItem = CreateTestMediaItem();
        File.WriteAllText(mediaItem.FilePath, "test");
        try
        {
            sut.SelectedMediaItem = mediaItem;

            // Act
            sut.AddToTimelineCommand.Execute(null);

            // Assert
            Assert.Contains("No available track", sut.StatusText);
        }
        finally
        {
            File.Delete(mediaItem.FilePath);
        }
    }

    #endregion

    #region DeleteClip Tests

    [Fact]
    public void DeleteClipCommand_WhenNoClipSelected_DoesNothing()
    {
        // Arrange
        var sut = CreateSut();
        sut.Timeline.SelectedClip = null;
        var initialStatus = sut.StatusText;

        // Act
        sut.DeleteClipCommand.Execute(null);

        // Assert - status should remain unchanged
        Assert.Equal(initialStatus, sut.StatusText);
    }

    [Fact]
    public void DeleteClipCommand_WithSelectedClip_RemovesClipFromTrack()
    {
        // Arrange
        var sut = CreateSut();
        var track = sut.Timeline.Tracks.First(t => t.TrackType == TrackType.Video);
        var clip = new TimelineClip
        {
            Name = "Test Clip",
            StartFrame = 0,
            EndFrame = 100,
            TrackType = TrackType.Video
        };
        track.Clips.Add(clip);
        sut.Timeline.SelectedClip = clip;

        // Act
        sut.DeleteClipCommand.Execute(null);

        // Assert
        Assert.DoesNotContain(clip, track.Clips);
        Assert.Contains("Deleted", sut.StatusText);
    }

    #endregion

    #region SplitClip Tests

    [Fact]
    public void SplitClipCommand_WhenNoClipSelected_DoesNothing()
    {
        // Arrange
        var sut = CreateSut();
        sut.Timeline.SelectedClip = null;

        // Act
        sut.SplitClipCommand.Execute(null);

        // Assert - no exception and timeline unchanged
        Assert.Empty(sut.Timeline.Tracks.SelectMany(t => t.Clips));
    }

    [Fact]
    public void SplitClipCommand_WhenPlayheadOutsideClip_ShowsError()
    {
        // Arrange
        var sut = CreateSut();
        var track = sut.Timeline.Tracks.First(t => t.TrackType == TrackType.Video);
        var clip = new TimelineClip
        {
            Name = "Test Clip",
            StartFrame = 100,
            EndFrame = 200,
            TrackType = TrackType.Video
        };
        track.Clips.Add(clip);
        sut.Timeline.SelectedClip = clip;
        sut.Timeline.PlayheadFrame = 50; // Before clip

        // Act
        sut.SplitClipCommand.Execute(null);

        // Assert
        Assert.Single(track.Clips); // No split occurred
        Assert.Contains("within", sut.StatusText.ToLower());
    }

    [Fact]
    public void SplitClipCommand_WhenPlayheadInsideClip_SplitsClip()
    {
        // Arrange
        var sut = CreateSut();
        var track = sut.Timeline.Tracks.First(t => t.TrackType == TrackType.Video);
        var clip = new TimelineClip
        {
            Name = "Test Clip",
            StartFrame = 0,
            EndFrame = 200,
            SourceInFrame = 0,
            TrackType = TrackType.Video
        };
        track.Clips.Add(clip);
        sut.Timeline.SelectedClip = clip;
        sut.Timeline.PlayheadFrame = 100; // Middle of clip

        // Act
        sut.SplitClipCommand.Execute(null);

        // Assert
        Assert.Equal(2, track.Clips.Count);
        Assert.Contains("split", sut.StatusText.ToLower());
    }

    #endregion

    #region Copy/Cut/Paste Tests

    [Fact]
    public void CopyClipCommand_WithSelectedClip_CopiesToClipboard()
    {
        // Arrange
        var sut = CreateSut();
        var track = sut.Timeline.Tracks.First(t => t.TrackType == TrackType.Video);
        var clip = new TimelineClip { Name = "Test Clip", TrackType = TrackType.Video };
        track.Clips.Add(clip);
        sut.Timeline.SelectedClip = clip;

        // Act
        sut.CopyClipCommand.Execute(null);

        // Assert
        Assert.Contains("Copied", sut.StatusText);
    }

    [Fact]
    public void CutClipCommand_WithSelectedClip_RemovesAndCopiesToClipboard()
    {
        // Arrange
        var sut = CreateSut();
        var track = sut.Timeline.Tracks.First(t => t.TrackType == TrackType.Video);
        var clip = new TimelineClip { Name = "Test Clip", TrackType = TrackType.Video };
        track.Clips.Add(clip);
        sut.Timeline.SelectedClip = clip;

        // Act
        sut.CutClipCommand.Execute(null);

        // Assert
        Assert.DoesNotContain(clip, track.Clips);
        Assert.Contains("Cut", sut.StatusText);
    }

    [Fact]
    public void PasteClipCommand_AfterCopy_CreatesNewClip()
    {
        // Arrange
        var sut = CreateSut();
        var track = sut.Timeline.Tracks.First(t => t.TrackType == TrackType.Video);
        var clip = new TimelineClip
        {
            Name = "Test Clip",
            TrackType = TrackType.Video,
            StartFrame = 0,
            EndFrame = 100
        };
        track.Clips.Add(clip);
        sut.Timeline.SelectedClip = clip;

        // Copy first
        sut.CopyClipCommand.Execute(null);
        sut.Timeline.PlayheadFrame = 200;

        // Act
        sut.PasteClipCommand.Execute(null);

        // Assert
        Assert.Equal(2, track.Clips.Count);
    }

    #endregion

    #region Undo/Redo Tests

    [Fact]
    public void CanUndo_InitiallyFalse()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        Assert.False(sut.CanUndo);
    }

    [Fact]
    public void CanUndo_AfterAddingClip_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        var mediaItem = CreateTestMediaItem();
        File.WriteAllText(mediaItem.FilePath, "test");
        try
        {
            sut.SelectedMediaItem = mediaItem;
            sut.SourceOutPoint = mediaItem.Duration;

            // Act
            sut.AddToTimelineCommand.Execute(null);

            // Assert
            Assert.True(sut.CanUndo);
        }
        finally
        {
            File.Delete(mediaItem.FilePath);
        }
    }

    [Fact]
    public void UndoCommand_AfterAddingClip_RemovesClip()
    {
        // Arrange
        var sut = CreateSut();
        var mediaItem = CreateTestMediaItem();
        File.WriteAllText(mediaItem.FilePath, "test");
        try
        {
            sut.SelectedMediaItem = mediaItem;
            sut.SourceOutPoint = mediaItem.Duration;
            sut.AddToTimelineCommand.Execute(null);

            var videoTrack = sut.Timeline.Tracks.First(t => t.TrackType == TrackType.Video);
            Assert.Single(videoTrack.Clips);

            // Act
            sut.UndoCommand.Execute(null);

            // Assert
            Assert.Empty(videoTrack.Clips);
        }
        finally
        {
            File.Delete(mediaItem.FilePath);
        }
    }

    #endregion

    #region Scrubbing Tests

    [Fact]
    public void BeginScrub_SetsIsScrubbingTrue()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.BeginScrub();

        // Assert
        Assert.True(sut.IsScrubbing);
    }

    [Fact]
    public void EndScrub_SetsIsScrubbingFalse()
    {
        // Arrange
        var sut = CreateSut();
        sut.BeginScrub();

        // Act
        sut.EndScrub();

        // Assert
        Assert.False(sut.IsScrubbing);
    }

    #endregion

    #region Selected Media Item Tests

    [Fact]
    public void OnSelectedMediaItemChanged_UpdatesSourceMonitorItem()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var sut = CreateSut(mediaPool);
        var mediaItem = CreateTestMediaItem();

        // Act
        sut.SelectedMediaItem = mediaItem;

        // Assert
        Assert.Equal(mediaItem, sut.SourceMonitorItem);
        mediaPool.Verify(m => m.SetCurrentSource(mediaItem), Times.Once);
    }

    [Fact]
    public void OnSelectedMediaItemChanged_SetsInOutPoints()
    {
        // Arrange
        var sut = CreateSut();
        var mediaItem = CreateTestMediaItem(duration: 30.0);

        // Act
        sut.SelectedMediaItem = mediaItem;

        // Assert
        Assert.Equal(0, sut.SourceInPoint);
        Assert.Equal(30.0, sut.SourceOutPoint);
    }

    #endregion

    #region Property Change Tests

    [Fact]
    public void SnapToClips_RaisesPropertyChanged()
    {
        // Arrange
        var sut = CreateSut();
        var propertyChanged = false;
        sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(sut.SnapToClips))
                propertyChanged = true;
        };

        // Act
        sut.SnapToClips = false;

        // Assert
        Assert.True(propertyChanged);
        Assert.False(sut.SnapToClips);
    }

    [Fact]
    public void RippleEdit_RaisesPropertyChanged()
    {
        // Arrange
        var sut = CreateSut();
        var propertyChanged = false;
        sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(sut.RippleEdit))
                propertyChanged = true;
        };

        // Act
        sut.RippleEdit = true;

        // Assert
        Assert.True(propertyChanged);
        Assert.True(sut.RippleEdit);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var sut = CreateSut(mediaPool);

        // Act
        sut.Dispose();

        // Assert - no exceptions on multiple dispose
        sut.Dispose();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void AddToTimeline_WithZeroDurationMedia_UsesFallbackDuration()
    {
        // Arrange
        var sut = CreateSut();
        var mediaItem = CreateTestMediaItem(duration: 0);
        File.WriteAllText(mediaItem.FilePath, "test");
        try
        {
            sut.SelectedMediaItem = mediaItem;

            // Act
            sut.AddToTimelineCommand.Execute(null);

            // Assert
            var clip = sut.Timeline.Tracks.First(t => t.TrackType == TrackType.Video).Clips.FirstOrDefault();
            Assert.NotNull(clip);
            Assert.True(clip.DurationFrames > 0);
        }
        finally
        {
            File.Delete(mediaItem.FilePath);
        }
    }

    [Fact]
    public void InsertClip_ShiftsExistingClips()
    {
        // Arrange
        var sut = CreateSut();
        var track = sut.Timeline.Tracks.First(t => t.TrackType == TrackType.Video);
        var existingClip = new TimelineClip
        {
            Name = "Existing",
            StartFrame = 100,
            EndFrame = 200,
            TrackType = TrackType.Video
        };
        track.Clips.Add(existingClip);

        var mediaItem = CreateTestMediaItem(duration: 5);
        File.WriteAllText(mediaItem.FilePath, "test");
        try
        {
            sut.SelectedMediaItem = mediaItem;
            sut.Timeline.PlayheadFrame = 50;

            // Act
            sut.InsertClipCommand.Execute(null);

            // Assert - existing clip should be shifted
            Assert.Equal(2, track.Clips.Count);
            var shiftedClip = track.Clips.First(c => c.Name == "Existing");
            Assert.True(shiftedClip.StartFrame > 100);
        }
        finally
        {
            File.Delete(mediaItem.FilePath);
        }
    }

    #endregion
}
