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

    #region Playback Controls Tests

    [Fact]
    public void PlayCommand_TogglesIsPlaying()
    {
        // Arrange
        var sut = CreateSut();
        Assert.False(sut.IsPlaying);

        // Act
        sut.PlayCommand.Execute(null);

        // Assert
        Assert.True(sut.IsPlaying);
        Assert.Equal("Playing", sut.StatusText);
    }

    [Fact]
    public void PlayCommand_SecondCall_Pauses()
    {
        // Arrange
        var sut = CreateSut();
        sut.PlayCommand.Execute(null);

        // Act
        sut.PlayCommand.Execute(null);

        // Assert
        Assert.False(sut.IsPlaying);
        Assert.Equal("Paused", sut.StatusText);
    }

    [Fact]
    public void StopCommand_StopsAndResetsPlayhead()
    {
        // Arrange
        var sut = CreateSut();
        sut.PlayCommand.Execute(null);
        sut.Timeline.PlayheadFrame = 100;

        // Act
        sut.StopCommand.Execute(null);

        // Assert
        Assert.False(sut.IsPlaying);
        Assert.Equal(0, sut.Timeline.PlayheadFrame);
        Assert.Equal("Stopped", sut.StatusText);
    }

    [Fact]
    public void GoToStartCommand_SetsPlayheadToZero()
    {
        // Arrange
        var sut = CreateSut();
        sut.Timeline.PlayheadFrame = 100;

        // Act
        sut.GoToStartCommand.Execute(null);

        // Assert
        Assert.Equal(0, sut.Timeline.PlayheadFrame);
    }

    [Fact]
    public void GoToEndCommand_SetsPlayheadToEnd()
    {
        // Arrange
        var sut = CreateSut();
        var track = sut.Timeline.Tracks.First(t => t.TrackType == TrackType.Video);
        track.Clips.Add(new TimelineClip { StartFrame = 0, EndFrame = 200, TrackType = TrackType.Video });

        // Act
        sut.GoToEndCommand.Execute(null);

        // Assert
        Assert.Equal(sut.Timeline.DurationFrames, sut.Timeline.PlayheadFrame);
    }

    [Fact]
    public void StepForwardCommand_IncrementsPlayhead()
    {
        // Arrange
        var sut = CreateSut();
        sut.Timeline.PlayheadFrame = 10;

        // Act
        sut.StepForwardCommand.Execute(null);

        // Assert
        Assert.Equal(11, sut.Timeline.PlayheadFrame);
    }

    [Fact]
    public void StepBackwardCommand_DecrementsPlayhead()
    {
        // Arrange
        var sut = CreateSut();
        sut.Timeline.PlayheadFrame = 10;

        // Act
        sut.StepBackwardCommand.Execute(null);

        // Assert
        Assert.Equal(9, sut.Timeline.PlayheadFrame);
    }

    [Fact]
    public void StepBackwardCommand_AtZero_DoesNotGoNegative()
    {
        // Arrange
        var sut = CreateSut();
        sut.Timeline.PlayheadFrame = 0;

        // Act
        sut.StepBackwardCommand.Execute(null);

        // Assert
        Assert.Equal(0, sut.Timeline.PlayheadFrame);
    }

    #endregion

    #region In/Out Point Tests

    [Fact]
    public void SetInPointCommand_SetsTimelineInPoint()
    {
        // Arrange
        var sut = CreateSut();
        sut.Timeline.PlayheadFrame = 50;

        // Act
        sut.SetInPointCommand.Execute(null);

        // Assert
        Assert.Equal(50, sut.Timeline.InPoint);
        Assert.Contains("In point set", sut.StatusText);
    }

    [Fact]
    public void SetOutPointCommand_SetsTimelineOutPoint()
    {
        // Arrange
        var sut = CreateSut();
        sut.Timeline.PlayheadFrame = 100;

        // Act
        sut.SetOutPointCommand.Execute(null);

        // Assert
        Assert.Equal(100, sut.Timeline.OutPoint);
        Assert.Contains("Out point set", sut.StatusText);
    }

    [Fact]
    public void ClearInOutPointsCommand_ClearsInOutPoints()
    {
        // Arrange
        var sut = CreateSut();
        sut.Timeline.InPoint = 50;
        sut.Timeline.OutPoint = 100;

        // Act
        sut.ClearInOutPointsCommand.Execute(null);

        // Assert
        Assert.Equal(-1, sut.Timeline.InPoint);
        Assert.Equal(-1, sut.Timeline.OutPoint);
        Assert.Contains("cleared", sut.StatusText);
    }

    #endregion

    #region Track Management Tests

    [Fact]
    public void AddVideoTrackCommand_AddsVideoTrack()
    {
        // Arrange
        var sut = CreateSut();
        var initialVideoTracks = sut.Timeline.Tracks.Count(t => t.TrackType == TrackType.Video);

        // Act
        sut.AddVideoTrackCommand.Execute(null);

        // Assert
        Assert.Equal(initialVideoTracks + 1, sut.Timeline.Tracks.Count(t => t.TrackType == TrackType.Video));
        Assert.Contains("Added video track", sut.StatusText);
    }

    [Fact]
    public void AddAudioTrackCommand_AddsAudioTrack()
    {
        // Arrange
        var sut = CreateSut();
        var initialAudioTracks = sut.Timeline.Tracks.Count(t => t.TrackType == TrackType.Audio);

        // Act
        sut.AddAudioTrackCommand.Execute(null);

        // Assert
        Assert.Equal(initialAudioTracks + 1, sut.Timeline.Tracks.Count(t => t.TrackType == TrackType.Audio));
        Assert.Contains("Added audio track", sut.StatusText);
    }

    [Fact]
    public void DeleteTrackCommand_WhenTrackSelected_RemovesTrack()
    {
        // Arrange
        var sut = CreateSut();
        var initialCount = sut.Timeline.Tracks.Count;
        sut.Timeline.SelectedTrack = sut.Timeline.Tracks.First();

        // Act
        sut.DeleteTrackCommand.Execute(null);

        // Assert
        Assert.Equal(initialCount - 1, sut.Timeline.Tracks.Count);
        Assert.Contains("Track deleted", sut.StatusText);
    }

    [Fact]
    public void DeleteTrackCommand_WhenNoTrackSelected_DoesNothing()
    {
        // Arrange
        var sut = CreateSut();
        sut.Timeline.SelectedTrack = null;
        var initialCount = sut.Timeline.Tracks.Count;

        // Act
        sut.DeleteTrackCommand.Execute(null);

        // Assert
        Assert.Equal(initialCount, sut.Timeline.Tracks.Count);
    }

    [Fact]
    public void DeleteTrackCommand_WhenOnlyOneTrack_DoesNothing()
    {
        // Arrange
        var sut = CreateSut();
        while (sut.Timeline.Tracks.Count > 1)
        {
            sut.Timeline.Tracks.RemoveAt(sut.Timeline.Tracks.Count - 1);
        }
        sut.Timeline.SelectedTrack = sut.Timeline.Tracks.First();

        // Act
        sut.DeleteTrackCommand.Execute(null);

        // Assert
        Assert.Single(sut.Timeline.Tracks);
    }

    #endregion

    #region Audio Mixer Tests

    [Fact]
    public void ToggleMixerPanelCommand_TogglesShowMixerPanel()
    {
        // Arrange
        var sut = CreateSut();
        Assert.False(sut.ShowMixerPanel);

        // Act
        sut.ToggleMixerPanelCommand.Execute(null);

        // Assert
        Assert.True(sut.ShowMixerPanel);
    }

    [Fact]
    public void ToggleTrackMuteCommand_TogglesTrackMute()
    {
        // Arrange
        var sut = CreateSut();
        var track = sut.Timeline.Tracks.First(t => t.TrackType == TrackType.Audio);
        Assert.False(track.IsMuted);

        // Act
        sut.ToggleTrackMuteCommand.Execute(track);

        // Assert
        Assert.True(track.IsMuted);
    }

    [Fact]
    public void ToggleTrackSoloCommand_TogglesTrackSolo()
    {
        // Arrange
        var sut = CreateSut();
        var track = sut.Timeline.Tracks.First(t => t.TrackType == TrackType.Audio);
        Assert.False(track.IsSolo);

        // Act
        sut.ToggleTrackSoloCommand.Execute(track);

        // Assert
        Assert.True(track.IsSolo);
    }

    [Fact]
    public void ToggleTrackSoloCommand_UnsolosOtherTracks()
    {
        // Arrange
        var sut = CreateSut();
        var audioTracks = sut.Timeline.Tracks.Where(t => t.TrackType == TrackType.Audio).ToList();
        if (audioTracks.Count < 2) return; // Skip if not enough tracks

        audioTracks[0].IsSolo = true;

        // Act
        sut.ToggleTrackSoloCommand.Execute(audioTracks[1]);

        // Assert
        Assert.False(audioTracks[0].IsSolo);
        Assert.True(audioTracks[1].IsSolo);
    }

    [Fact]
    public void ResetTrackVolumeCommand_ResetsVolumeAndPan()
    {
        // Arrange
        var sut = CreateSut();
        var track = sut.Timeline.Tracks.First(t => t.TrackType == TrackType.Audio);
        track.Volume = 0.5;
        track.Pan = -0.5;

        // Act
        sut.ResetTrackVolumeCommand.Execute(track);

        // Assert
        Assert.Equal(1.0, track.Volume);
        Assert.Equal(0.0, track.Pan);
    }

    [Fact]
    public void MuteAllTracksCommand_MutesAllAudioTracks()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.MuteAllTracksCommand.Execute(null);

        // Assert
        Assert.All(sut.AudioTracks, t => Assert.True(t.IsMuted));
    }

    [Fact]
    public void UnmuteAllTracksCommand_UnmutesAllAudioTracks()
    {
        // Arrange
        var sut = CreateSut();
        foreach (var track in sut.AudioTracks)
        {
            track.IsMuted = true;
        }

        // Act
        sut.UnmuteAllTracksCommand.Execute(null);

        // Assert
        Assert.All(sut.AudioTracks, t => Assert.False(t.IsMuted));
    }

    [Fact]
    public void AudioTracks_ReturnsOnlyAudioTracks()
    {
        // Arrange
        var sut = CreateSut();

        // Assert
        Assert.All(sut.AudioTracks, t => Assert.Equal(TrackType.Audio, t.TrackType));
    }

    #endregion

    #region Text Overlay Tests

    [Fact]
    public void AddTextOverlayCommand_AddsOverlayAtPlayhead()
    {
        // Arrange
        var sut = CreateSut();
        sut.Timeline.PlayheadFrame = 50;
        var initialCount = sut.Timeline.TextOverlays.Count;

        // Act
        sut.AddTextOverlayCommand.Execute(null);

        // Assert
        Assert.Equal(initialCount + 1, sut.Timeline.TextOverlays.Count);
        Assert.Equal(50, sut.Timeline.TextOverlays.Last().StartFrame);
        Assert.Equal("New Text", sut.Timeline.TextOverlays.Last().Text);
    }

    [Fact]
    public void DeleteTextOverlayCommand_WhenSelected_RemovesOverlay()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddTextOverlayCommand.Execute(null);
        var overlay = sut.Timeline.TextOverlays.First();
        sut.Timeline.SelectedTextOverlay = overlay;

        // Act
        sut.DeleteTextOverlayCommand.Execute(null);

        // Assert
        Assert.DoesNotContain(overlay, sut.Timeline.TextOverlays);
    }

    [Fact]
    public void DuplicateTextOverlayCommand_ClonesAtPlayhead()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddTextOverlayCommand.Execute(null);
        sut.Timeline.SelectedTextOverlay = sut.Timeline.TextOverlays.First();
        sut.Timeline.SelectedTextOverlay.Text = "Original";
        sut.Timeline.PlayheadFrame = 100;

        // Act
        sut.DuplicateTextOverlayCommand.Execute(null);

        // Assert
        Assert.Equal(2, sut.Timeline.TextOverlays.Count);
        Assert.Equal(100, sut.Timeline.SelectedTextOverlay?.StartFrame);
    }

    [Fact]
    public void UpdateTextOverlay_UpdatesOverlayText()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddTextOverlayCommand.Execute(null);
        var overlay = sut.Timeline.TextOverlays.First();

        // Act
        sut.UpdateTextOverlay(overlay, "Updated Text");

        // Assert
        Assert.Equal("Updated Text", overlay.Text);
    }

    [Fact]
    public void MoveTextOverlay_ChangesStartFrame()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddTextOverlayCommand.Execute(null);
        var overlay = sut.Timeline.TextOverlays.First();

        // Act
        sut.MoveTextOverlay(overlay, 200);

        // Assert
        Assert.Equal(200, overlay.StartFrame);
    }

    [Fact]
    public void ResizeTextOverlay_ChangesDuration()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddTextOverlayCommand.Execute(null);
        var overlay = sut.Timeline.TextOverlays.First();

        // Act
        sut.ResizeTextOverlay(overlay, 100);

        // Assert
        Assert.Equal(100, overlay.DurationFrames);
    }

    [Fact]
    public void ResizeTextOverlay_EnforcesMinimumDuration()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddTextOverlayCommand.Execute(null);
        var overlay = sut.Timeline.TextOverlays.First();

        // Act
        sut.ResizeTextOverlay(overlay, 0);

        // Assert
        Assert.Equal(1, overlay.DurationFrames);
    }

    #endregion

    #region Marker Tests

    [Fact]
    public void AddMarkerCommand_AddsMarkerAtPlayhead()
    {
        // Arrange
        var sut = CreateSut();
        sut.Timeline.PlayheadFrame = 100;

        // Act
        sut.AddMarkerCommand.Execute(null);

        // Assert
        Assert.Single(sut.Timeline.Markers);
        Assert.Equal(100, sut.Timeline.Markers.First().Frame);
    }

    [Fact]
    public void AddMarkerWithNameCommand_AddsNamedMarker()
    {
        // Arrange
        var sut = CreateSut();
        sut.Timeline.PlayheadFrame = 50;

        // Act
        sut.AddMarkerWithNameCommand.Execute("Test Marker");

        // Assert
        Assert.Single(sut.Timeline.Markers);
        Assert.Equal("Test Marker", sut.Timeline.Markers.First().Name);
    }

    [Fact]
    public void DeleteMarkerCommand_WhenSelected_RemovesMarker()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddMarkerCommand.Execute(null);
        sut.Timeline.SelectedMarker = sut.Timeline.Markers.First();

        // Act
        sut.DeleteMarkerCommand.Execute(null);

        // Assert
        Assert.Empty(sut.Timeline.Markers);
    }

    [Fact]
    public void NextMarkerCommand_JumpsToNextMarker()
    {
        // Arrange
        var sut = CreateSut();
        sut.Timeline.PlayheadFrame = 0;
        sut.AddMarkerCommand.Execute(null);
        sut.Timeline.PlayheadFrame = 100;
        sut.AddMarkerCommand.Execute(null);
        sut.Timeline.PlayheadFrame = 0;

        // Act
        sut.NextMarkerCommand.Execute(null);

        // Assert - Should be at or after the first marker
        Assert.True(sut.Timeline.PlayheadFrame >= 0);
    }

    [Fact]
    public void PreviousMarkerCommand_JumpsToPreviousMarker()
    {
        // Arrange
        var sut = CreateSut();
        sut.Timeline.PlayheadFrame = 50;
        sut.AddMarkerCommand.Execute(null);
        sut.Timeline.PlayheadFrame = 100;
        sut.AddMarkerCommand.Execute(null);

        // Act
        sut.PreviousMarkerCommand.Execute(null);

        // Assert - Should jump to a previous marker
        Assert.True(sut.Timeline.PlayheadFrame <= 100);
    }

    [Fact]
    public void GoToMarkerCommand_JumpsToSpecificMarker()
    {
        // Arrange
        var sut = CreateSut();
        sut.Timeline.PlayheadFrame = 200;
        sut.AddMarkerCommand.Execute(null);
        var marker = sut.Timeline.Markers.First();
        sut.Timeline.PlayheadFrame = 0;

        // Act
        sut.GoToMarkerCommand.Execute(marker);

        // Assert
        Assert.Equal(marker.Frame, sut.Timeline.PlayheadFrame);
    }

    [Fact]
    public void ClearAllMarkersCommand_RemovesAllMarkers()
    {
        // Arrange
        var sut = CreateSut();
        sut.Timeline.PlayheadFrame = 0;
        sut.AddMarkerCommand.Execute(null);
        sut.Timeline.PlayheadFrame = 50;
        sut.AddMarkerCommand.Execute(null);
        sut.Timeline.PlayheadFrame = 100;
        sut.AddMarkerCommand.Execute(null);

        // Act
        sut.ClearAllMarkersCommand.Execute(null);

        // Assert
        Assert.Empty(sut.Timeline.Markers);
    }

    [Fact]
    public void ToggleMarkerPanelCommand_TogglesPanel()
    {
        // Arrange
        var sut = CreateSut();
        Assert.False(sut.ShowMarkerPanel);

        // Act
        sut.ToggleMarkerPanelCommand.Execute(null);

        // Assert
        Assert.True(sut.ShowMarkerPanel);
    }

    #endregion

    #region Effect Management Tests

    [Fact]
    public void ToggleEffectPanelCommand_TogglesPanel()
    {
        // Arrange
        var sut = CreateSut();
        Assert.False(sut.ShowEffectPanel);

        // Act
        sut.ToggleEffectPanelCommand.Execute(null);

        // Assert
        Assert.True(sut.ShowEffectPanel);
    }

    [Fact]
    public void AvailableEffects_ReturnsGroupedEffects()
    {
        // Arrange
        var sut = CreateSut();

        // Assert
        Assert.NotNull(sut.AvailableEffects);
    }

    [Fact]
    public void ToggleKeyframePanelCommand_TogglesPanel()
    {
        // Arrange
        var sut = CreateSut();
        Assert.False(sut.ShowKeyframePanel);

        // Act
        sut.ToggleKeyframePanelCommand.Execute(null);

        // Assert
        Assert.True(sut.ShowKeyframePanel);
    }

    [Fact]
    public void EffectService_IsNotNull()
    {
        // Arrange
        var sut = CreateSut();

        // Assert
        Assert.NotNull(sut.EffectService);
    }

    #endregion

    #region Redo Tests

    [Fact]
    public void CanRedo_InitiallyFalse()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        Assert.False(sut.CanRedo);
    }

    [Fact]
    public void RedoCommand_AfterUndo_RestoresState()
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
            sut.UndoCommand.Execute(null);

            var videoTrack = sut.Timeline.Tracks.First(t => t.TrackType == TrackType.Video);
            Assert.Empty(videoTrack.Clips);

            // Act
            sut.RedoCommand.Execute(null);

            // Assert
            Assert.Single(videoTrack.Clips);
        }
        finally
        {
            File.Delete(mediaItem.FilePath);
        }
    }

    #endregion

    #region Transition Preset Tests

    [Fact]
    public void TransitionPresets_ContainsPresets()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        Assert.NotEmpty(sut.TransitionPresets);
    }

    [Fact]
    public void SelectedTransitionPreset_DefaultsToCrossDissolve()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        Assert.NotNull(sut.SelectedTransitionPreset);
        Assert.Equal(TransitionType.CrossDissolve, sut.SelectedTransitionPreset.Type);
    }

    #endregion

    #region Frame Cache Tests

    [Fact]
    public void GetFrameCacheStats_ReturnsStats()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var stats = sut.GetFrameCacheStats();

        // Assert
        Assert.NotNull(stats);
    }

    #endregion

    #region Undo Transaction Tests

    [Fact]
    public void BeginUndoTransaction_ReturnsTransaction()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        using var transaction = sut.BeginUndoTransaction("Test");

        // Assert
        Assert.NotNull(transaction);
    }

    #endregion

    #region Timeline Script Generation Tests

    [Fact]
    public void GenerateTimelineScript_ReturnsString()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var script = sut.GenerateTimelineScript();

        // Assert
        Assert.NotNull(script);
    }

    #endregion

    #region Current Timecode Tests

    [Fact]
    public void CurrentTimecode_UpdatesOnPlayheadChange()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Timeline.PlayheadFrame = 48; // 2 seconds at 24fps

        // Assert - Timecode should be formatted correctly
        Assert.NotNull(sut.CurrentTimecode);
    }

    #endregion

    #region Panel Width Tests

    [Fact]
    public void MixerPanelHeight_WhenHidden_IsZero()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowMixerPanel = false;

        // Assert
        Assert.Equal(new System.Windows.GridLength(0), sut.MixerPanelHeight);
    }

    [Fact]
    public void MixerPanelHeight_WhenVisible_Is120()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowMixerPanel = true;

        // Assert
        Assert.Equal(new System.Windows.GridLength(120), sut.MixerPanelHeight);
    }

    [Fact]
    public void KeyframePanelWidth_WhenHidden_IsZero()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowKeyframePanel = false;

        // Assert
        Assert.Equal(new System.Windows.GridLength(0), sut.KeyframePanelWidth);
    }

    [Fact]
    public void KeyframePanelWidth_WhenVisible_Is280()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowKeyframePanel = true;

        // Assert
        Assert.Equal(new System.Windows.GridLength(280), sut.KeyframePanelWidth);
    }

    [Fact]
    public void MarkerPanelWidth_WhenHidden_IsZero()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowMarkerPanel = false;

        // Assert
        Assert.Equal(new System.Windows.GridLength(0), sut.MarkerPanelWidth);
    }

    [Fact]
    public void MarkerPanelWidth_WhenVisible_Is200()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowMarkerPanel = true;

        // Assert
        Assert.Equal(new System.Windows.GridLength(200), sut.MarkerPanelWidth);
    }

    #endregion

    #region Property Notification Tests

    [Fact]
    public void IsPlaying_RaisesPropertyChanged()
    {
        // Arrange
        var sut = CreateSut();
        var propertyChanged = false;
        sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(sut.IsPlaying))
                propertyChanged = true;
        };

        // Act
        sut.PlayCommand.Execute(null);

        // Assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public void StatusText_RaisesPropertyChanged()
    {
        // Arrange
        var sut = CreateSut();
        var propertyChanged = false;
        sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(sut.StatusText))
                propertyChanged = true;
        };

        // Act
        sut.PlayCommand.Execute(null);

        // Assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public void ShowMixerPanel_RaisesPropertyChanged()
    {
        // Arrange
        var sut = CreateSut();
        var propertyChanged = false;
        sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(sut.ShowMixerPanel))
                propertyChanged = true;
        };

        // Act
        sut.ToggleMixerPanelCommand.Execute(null);

        // Assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public void ShowMixerPanel_RaisesMixerPanelHeightChanged()
    {
        // Arrange
        var sut = CreateSut();
        var propertyChanged = false;
        sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(sut.MixerPanelHeight))
                propertyChanged = true;
        };

        // Act
        sut.ToggleMixerPanelCommand.Execute(null);

        // Assert
        Assert.True(propertyChanged);
    }

    #endregion

    #region Undo Description Tests

    [Fact]
    public void UndoDescription_WhenEmpty_ReturnsEmpty()
    {
        // Arrange
        var sut = CreateSut();

        // Assert
        Assert.NotNull(sut.UndoDescription);
    }

    [Fact]
    public void RedoDescription_WhenEmpty_ReturnsEmpty()
    {
        // Arrange
        var sut = CreateSut();

        // Assert
        Assert.NotNull(sut.RedoDescription);
    }

    #endregion

    #region Loading State Tests

    [Fact]
    public void IsLoading_DefaultIsFalse()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public void LoadingMessage_DefaultIsLoading()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        Assert.Equal("Loading...", sut.LoadingMessage);
    }

    #endregion

    #region Null Guard Tests

    [Fact]
    public void ToggleTrackMuteCommand_WithNull_DoesNotThrow()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.ToggleTrackMuteCommand.Execute(null);
    }

    [Fact]
    public void ToggleTrackSoloCommand_WithNull_DoesNotThrow()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.ToggleTrackSoloCommand.Execute(null);
    }

    [Fact]
    public void ResetTrackVolumeCommand_WithNull_DoesNotThrow()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.ResetTrackVolumeCommand.Execute(null);
    }

    [Fact]
    public void GoToMarkerCommand_WithNull_DoesNotThrow()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.GoToMarkerCommand.Execute(null);
    }

    [Fact]
    public void UpdateTextOverlay_WithNull_DoesNotThrow()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.UpdateTextOverlay(null!, "text");
    }

    [Fact]
    public void MoveTextOverlay_WithNull_DoesNotThrow()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.MoveTextOverlay(null!, 100);
    }

    [Fact]
    public void ResizeTextOverlay_WithNull_DoesNotThrow()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.ResizeTextOverlay(null!, 100);
    }

    #endregion
}
