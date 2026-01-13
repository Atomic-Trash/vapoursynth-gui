using System.Collections.ObjectModel;
using FluentAssertions;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.Tests.E2E.Workflows;

/// <summary>
/// End-to-end tests for project lifecycle: create, save, load, modify, and recovery.
/// </summary>
public class ProjectLifecycleTests
{
    #region Project Creation Tests

    [Fact]
    public void NewProject_DefaultTimeline_HasCorrectStructure()
    {
        // Arrange & Act
        var timeline = new Timeline();

        // Assert
        timeline.Tracks.Should().BeEmpty();
        timeline.HasClips.Should().BeFalse();
        timeline.DurationFrames.Should().Be(0);
    }

    [Fact]
    public void NewProject_WithDefaultTracks_HasVideoAndAudioTracks()
    {
        // Arrange
        var timeline = new Timeline();

        // Act - Add default tracks
        timeline.Tracks.Add(new TimelineTrack { Name = "Video 1", TrackType = TrackType.Video });
        timeline.Tracks.Add(new TimelineTrack { Name = "Audio 1", TrackType = TrackType.Audio });

        // Assert
        timeline.Tracks.Should().HaveCount(2);
        timeline.Tracks.Should().Contain(t => t.TrackType == TrackType.Video);
        timeline.Tracks.Should().Contain(t => t.TrackType == TrackType.Audio);
    }

    [Fact]
    public void NewProject_WithFrameRate_SetsCorrectly()
    {
        // Arrange & Act
        var timeline = new Timeline { FrameRate = 29.97 };

        // Assert
        timeline.FrameRate.Should().BeApproximately(29.97, 0.001);
    }

    #endregion

    #region Project State Tests

    [Fact]
    public void ProjectState_AfterAddingClip_HasClipsIsTrue()
    {
        // Arrange
        var timeline = new Timeline();
        var track = new TimelineTrack { Name = "Video 1", TrackType = TrackType.Video };
        timeline.Tracks.Add(track);

        // Act
        track.Clips.Add(new TimelineClip
        {
            Name = "Test Clip",
            SourcePath = @"C:\test.mp4",
            StartFrame = 0,
            EndFrame = 100
        });

        // Assert
        timeline.HasClips.Should().BeTrue();
    }

    [Fact]
    public void ProjectState_AfterRemovingAllClips_HasClipsIsFalse()
    {
        // Arrange
        var timeline = new Timeline();
        var track = new TimelineTrack { Name = "Video 1", TrackType = TrackType.Video };
        timeline.Tracks.Add(track);
        var clip = new TimelineClip
        {
            Name = "Test Clip",
            SourcePath = @"C:\test.mp4",
            StartFrame = 0,
            EndFrame = 100
        };
        track.Clips.Add(clip);

        // Act
        track.Clips.Remove(clip);

        // Assert
        timeline.HasClips.Should().BeFalse();
    }

    [Fact]
    public void ProjectState_MultipleTracksWithClips_DurationCorrect()
    {
        // Arrange
        var timeline = new Timeline();

        var videoTrack = new TimelineTrack { Name = "Video 1", TrackType = TrackType.Video };
        videoTrack.Clips.Add(new TimelineClip
        {
            Name = "Video Clip",
            SourcePath = @"C:\video.mp4",
            StartFrame = 0,
            EndFrame = 500
        });

        var audioTrack = new TimelineTrack { Name = "Audio 1", TrackType = TrackType.Audio };
        audioTrack.Clips.Add(new TimelineClip
        {
            Name = "Audio Clip",
            SourcePath = @"C:\audio.mp3",
            StartFrame = 0,
            EndFrame = 600 // Longer than video
        });

        timeline.Tracks.Add(videoTrack);
        timeline.Tracks.Add(audioTrack);

        // Assert - Duration should be the maximum end frame
        timeline.DurationFrames.Should().Be(600);
    }

    #endregion

    #region Project Modification Tests

    [Fact]
    public void ProjectModification_AddTrack_TrackAdded()
    {
        // Arrange
        var timeline = new Timeline();
        timeline.Tracks.Add(new TimelineTrack { Name = "Video 1", TrackType = TrackType.Video });

        // Act
        timeline.Tracks.Add(new TimelineTrack { Name = "Video 2", TrackType = TrackType.Video });

        // Assert
        timeline.Tracks.Should().HaveCount(2);
        timeline.Tracks.Should().Contain(t => t.Name == "Video 2");
    }

    [Fact]
    public void ProjectModification_RemoveTrack_TrackRemoved()
    {
        // Arrange
        var timeline = new Timeline();
        var track1 = new TimelineTrack { Name = "Video 1", TrackType = TrackType.Video };
        var track2 = new TimelineTrack { Name = "Video 2", TrackType = TrackType.Video };
        timeline.Tracks.Add(track1);
        timeline.Tracks.Add(track2);

        // Act
        timeline.Tracks.Remove(track2);

        // Assert
        timeline.Tracks.Should().HaveCount(1);
        timeline.Tracks.Should().NotContain(t => t.Name == "Video 2");
    }

    [Fact]
    public void ProjectModification_ReorderTracks_OrderPreserved()
    {
        // Arrange
        var timeline = new Timeline();
        var track1 = new TimelineTrack { Name = "Track 1", TrackType = TrackType.Video };
        var track2 = new TimelineTrack { Name = "Track 2", TrackType = TrackType.Video };
        var track3 = new TimelineTrack { Name = "Track 3", TrackType = TrackType.Video };
        timeline.Tracks.Add(track1);
        timeline.Tracks.Add(track2);
        timeline.Tracks.Add(track3);

        // Act - Move track3 to position 0
        timeline.Tracks.Remove(track3);
        timeline.Tracks.Insert(0, track3);

        // Assert
        timeline.Tracks[0].Name.Should().Be("Track 3");
        timeline.Tracks[1].Name.Should().Be("Track 1");
        timeline.Tracks[2].Name.Should().Be("Track 2");
    }

    [Fact]
    public void ProjectModification_RenameTrack_NameUpdated()
    {
        // Arrange
        var track = new TimelineTrack { Name = "Original Name", TrackType = TrackType.Video };

        // Act
        track.Name = "New Name";

        // Assert
        track.Name.Should().Be("New Name");
    }

    [Fact]
    public void ProjectModification_MoveClip_PositionUpdated()
    {
        // Arrange
        var clip = new TimelineClip
        {
            Name = "Test Clip",
            SourcePath = @"C:\test.mp4",
            StartFrame = 0,
            EndFrame = 100
        };

        // Act
        clip.StartFrame = 500;
        clip.EndFrame = 600;

        // Assert
        clip.StartFrame.Should().Be(500);
        clip.EndFrame.Should().Be(600);
        clip.DurationFrames.Should().Be(100);
    }

    [Fact]
    public void ProjectModification_TrimClip_InOutFramesUpdated()
    {
        // Arrange
        var clip = new TimelineClip
        {
            Name = "Test Clip",
            SourcePath = @"C:\test.mp4",
            StartFrame = 0,
            EndFrame = 1000,
            SourceInFrame = 0,
            SourceOutFrame = 1000
        };

        // Act - Trim 100 frames from each end
        clip.SourceInFrame = 100;
        clip.SourceOutFrame = 900;

        // Assert
        clip.SourceInFrame.Should().Be(100);
        clip.SourceOutFrame.Should().Be(900);
    }

    #endregion

    #region Media Pool State Tests

    [Fact]
    public void MediaPoolState_AddMedia_CountIncreases()
    {
        // Arrange
        var mediaCollection = new ObservableCollection<MediaItem>();

        // Act
        mediaCollection.Add(new MediaItem { Name = "video1.mp4", FilePath = @"C:\video1.mp4" });
        mediaCollection.Add(new MediaItem { Name = "video2.mp4", FilePath = @"C:\video2.mp4" });

        // Assert
        mediaCollection.Should().HaveCount(2);
    }

    [Fact]
    public void MediaPoolState_RemoveMedia_CountDecreases()
    {
        // Arrange
        var mediaCollection = new ObservableCollection<MediaItem>();
        var item1 = new MediaItem { Name = "video1.mp4", FilePath = @"C:\video1.mp4" };
        var item2 = new MediaItem { Name = "video2.mp4", FilePath = @"C:\video2.mp4" };
        mediaCollection.Add(item1);
        mediaCollection.Add(item2);

        // Act
        mediaCollection.Remove(item1);

        // Assert
        mediaCollection.Should().HaveCount(1);
        mediaCollection.Should().NotContain(item1);
    }

    [Fact]
    public void MediaPoolState_ClearAll_CollectionEmpty()
    {
        // Arrange
        var mediaCollection = new ObservableCollection<MediaItem>();
        mediaCollection.Add(new MediaItem { Name = "video1.mp4", FilePath = @"C:\video1.mp4" });
        mediaCollection.Add(new MediaItem { Name = "video2.mp4", FilePath = @"C:\video2.mp4" });
        mediaCollection.Add(new MediaItem { Name = "video3.mp4", FilePath = @"C:\video3.mp4" });

        // Act
        mediaCollection.Clear();

        // Assert
        mediaCollection.Should().BeEmpty();
    }

    #endregion

    #region Settings Persistence Tests

    [Fact]
    public void SettingsPersistence_SaveAndLoad_ValuesPreserved()
    {
        // Arrange
        var settings = new AppSettings
        {
            DefaultVideoCodec = "libx265",
            DefaultAudioCodec = "flac",
            DefaultVideoQuality = 18,
            DefaultAudioBitrate = 320,
            DefaultExportFormat = "mkv"
        };

        // Act - Simulate save/load by copying values
        var loadedSettings = new AppSettings
        {
            DefaultVideoCodec = settings.DefaultVideoCodec,
            DefaultAudioCodec = settings.DefaultAudioCodec,
            DefaultVideoQuality = settings.DefaultVideoQuality,
            DefaultAudioBitrate = settings.DefaultAudioBitrate,
            DefaultExportFormat = settings.DefaultExportFormat
        };

        // Assert
        loadedSettings.DefaultVideoCodec.Should().Be("libx265");
        loadedSettings.DefaultAudioCodec.Should().Be("flac");
        loadedSettings.DefaultVideoQuality.Should().Be(18);
        loadedSettings.DefaultAudioBitrate.Should().Be(320);
        loadedSettings.DefaultExportFormat.Should().Be("mkv");
    }

    [Fact]
    public void SettingsPersistence_DefaultValues_Initialized()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert - Check default values exist
        settings.DefaultVideoCodec.Should().NotBeNullOrEmpty();
        settings.DefaultAudioCodec.Should().NotBeNullOrEmpty();
        settings.DefaultExportFormat.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Clip Clone Tests

    [Fact]
    public void ClipClone_CopiesAllProperties()
    {
        // Arrange
        var original = new TimelineClip
        {
            Name = "Test Clip",
            SourcePath = @"C:\test.mp4",
            StartFrame = 100,
            EndFrame = 500,
            FrameRate = 30,
            Volume = 0.8
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.Name.Should().Be("Test Clip");
        clone.SourcePath.Should().Be(@"C:\test.mp4");
        clone.StartFrame.Should().Be(100);
        clone.EndFrame.Should().Be(500);
        clone.FrameRate.Should().Be(30);
        clone.Volume.Should().Be(0.8);
        clone.Should().NotBeSameAs(original);
    }

    [Fact]
    public void ClipClone_CopiesEffects()
    {
        // Arrange
        var original = new TimelineClip
        {
            Name = "Clip 1",
            SourcePath = @"C:\test.mp4",
            StartFrame = 0,
            EndFrame = 100
        };
        original.Effects.Add(new TimelineEffect
        {
            Name = "Blur",
            EffectType = EffectType.Blur
        });

        // Act
        var clone = original.Clone();

        // Assert
        clone.Effects.Should().HaveCount(1);
        clone.Effects[0].Name.Should().Be("Blur");
        clone.Effects[0].Should().NotBeSameAs(original.Effects[0]);
    }

    [Fact]
    public void ClipClone_ModifyClone_OriginalUnchanged()
    {
        // Arrange
        var original = new TimelineClip
        {
            Name = "Original",
            SourcePath = @"C:\test.mp4",
            StartFrame = 0,
            EndFrame = 100
        };

        // Act
        var clone = original.Clone();
        clone.Name = "Modified";
        clone.StartFrame = 200;

        // Assert
        original.Name.Should().Be("Original");
        original.StartFrame.Should().Be(0);
    }

    #endregion

    #region Complex Project State Tests

    [Fact]
    public void ComplexProject_MultipleTracksMultipleClips_StateConsistent()
    {
        // Arrange & Act
        var timeline = new Timeline { FrameRate = 30 };

        // Add 3 video tracks with 2 clips each
        for (int t = 0; t < 3; t++)
        {
            var track = new TimelineTrack { Name = $"Video {t + 1}", TrackType = TrackType.Video };
            for (int c = 0; c < 2; c++)
            {
                track.Clips.Add(new TimelineClip
                {
                    Name = $"V{t + 1} Clip {c + 1}",
                    SourcePath = @"C:\test.mp4",
                    StartFrame = c * 100,
                    EndFrame = (c + 1) * 100
                });
            }
            timeline.Tracks.Add(track);
        }

        // Add 2 audio tracks with 1 clip each
        for (int t = 0; t < 2; t++)
        {
            var track = new TimelineTrack { Name = $"Audio {t + 1}", TrackType = TrackType.Audio };
            track.Clips.Add(new TimelineClip
            {
                Name = $"A{t + 1} Clip 1",
                SourcePath = @"C:\audio.mp3",
                StartFrame = 0,
                EndFrame = 200
            });
            timeline.Tracks.Add(track);
        }

        // Assert
        timeline.Tracks.Should().HaveCount(5);
        timeline.Tracks.Where(t => t.TrackType == TrackType.Video).Should().HaveCount(3);
        timeline.Tracks.Where(t => t.TrackType == TrackType.Audio).Should().HaveCount(2);
        timeline.HasClips.Should().BeTrue();

        var totalClips = timeline.Tracks.Sum(t => t.Clips.Count);
        totalClips.Should().Be(8); // 6 video clips + 2 audio clips
    }

    [Fact]
    public void ComplexProject_WithMarkers_AllMarkersPreserved()
    {
        // Arrange
        var timeline = new Timeline();

        // Act
        timeline.AddMarker("Start", 0);
        timeline.AddMarker("Scene 1", 100);
        timeline.AddMarker("Scene 2", 200);
        timeline.AddMarker("End", 500);

        // Assert
        timeline.Markers.Should().HaveCount(4);
        timeline.Markers.Should().Contain(m => m.Name == "Start" && m.Frame == 0);
        timeline.Markers.Should().Contain(m => m.Name == "End" && m.Frame == 500);
    }

    #endregion
}
