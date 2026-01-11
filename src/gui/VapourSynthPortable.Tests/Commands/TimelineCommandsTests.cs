using System.Collections.ObjectModel;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services.Commands;

namespace VapourSynthPortable.Tests.Commands;

public class TimelineCommandsTests
{
    private static TimelineClip CreateTestClip(string name = "Test Clip", long start = 0, long end = 100)
    {
        return new TimelineClip
        {
            Name = name,
            StartFrame = start,
            EndFrame = end,
            SourceInFrame = 0,
            SourceOutFrame = end - start,
            SourceDurationFrames = 1000,
            FrameRate = 24
        };
    }

    private static TimelineTrack CreateTestTrack(string name = "V1")
    {
        return new TimelineTrack(name, TrackType.Video);
    }

    private static Timeline CreateTestTimeline()
    {
        return new Timeline
        {
            Tracks = [CreateTestTrack()],
            FrameRate = 24
        };
    }

    #region AddClipCommand Tests

    [Fact]
    public void AddClipCommand_Redo_AddsClipToTrack()
    {
        // Arrange
        var track = CreateTestTrack();
        var clip = CreateTestClip();
        var command = new AddClipCommand(track, clip);

        // Act
        command.Redo();

        // Assert
        Assert.Single(track.Clips);
        Assert.Contains(clip, track.Clips);
    }

    [Fact]
    public void AddClipCommand_Undo_RemovesClipFromTrack()
    {
        // Arrange
        var track = CreateTestTrack();
        var clip = CreateTestClip();
        var command = new AddClipCommand(track, clip);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Empty(track.Clips);
    }

    [Fact]
    public void AddClipCommand_AtSpecificIndex_InsertsAtCorrectPosition()
    {
        // Arrange
        var track = CreateTestTrack();
        var clip1 = CreateTestClip("Clip 1");
        var clip2 = CreateTestClip("Clip 2");
        var clip3 = CreateTestClip("Clip 3");
        track.Clips.Add(clip1);
        track.Clips.Add(clip3);
        var command = new AddClipCommand(track, clip2, 1);

        // Act
        command.Redo();

        // Assert
        Assert.Equal(3, track.Clips.Count);
        Assert.Equal(clip2, track.Clips[1]);
    }

    [Fact]
    public void AddClipCommand_Description_IncludesClipName()
    {
        // Arrange
        var track = CreateTestTrack();
        var clip = CreateTestClip("My Video Clip");
        var command = new AddClipCommand(track, clip);

        // Assert
        Assert.Contains("My Video Clip", command.Description);
    }

    #endregion

    #region RemoveClipCommand Tests

    [Fact]
    public void RemoveClipCommand_Redo_RemovesClipFromTrack()
    {
        // Arrange
        var track = CreateTestTrack();
        var clip = CreateTestClip();
        track.Clips.Add(clip);
        var command = new RemoveClipCommand(track, clip);

        // Act
        command.Redo();

        // Assert
        Assert.Empty(track.Clips);
    }

    [Fact]
    public void RemoveClipCommand_Undo_RestoresClipAtOriginalIndex()
    {
        // Arrange
        var track = CreateTestTrack();
        var clip1 = CreateTestClip("Clip 1");
        var clip2 = CreateTestClip("Clip 2");
        var clip3 = CreateTestClip("Clip 3");
        track.Clips.Add(clip1);
        track.Clips.Add(clip2);
        track.Clips.Add(clip3);
        var command = new RemoveClipCommand(track, clip2);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Equal(3, track.Clips.Count);
        Assert.Equal(clip2, track.Clips[1]);
    }

    #endregion

    #region MoveClipCommand Tests

    [Fact]
    public void MoveClipCommand_Redo_MovesClipToNewPosition()
    {
        // Arrange
        var clip = CreateTestClip(start: 0, end: 100);
        var command = new MoveClipCommand(clip, 200);

        // Act
        command.Redo();

        // Assert
        Assert.Equal(200, clip.StartFrame);
        Assert.Equal(300, clip.EndFrame); // Duration preserved
    }

    [Fact]
    public void MoveClipCommand_Undo_RestoresOriginalPosition()
    {
        // Arrange
        var clip = CreateTestClip(start: 50, end: 150);
        var command = new MoveClipCommand(clip, 300);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Equal(50, clip.StartFrame);
        Assert.Equal(150, clip.EndFrame);
    }

    [Fact]
    public void MoveClipCommand_BetweenTracks_MovesClipCorrectly()
    {
        // Arrange
        var track1 = CreateTestTrack("V1");
        var track2 = CreateTestTrack("V2");
        var clip = CreateTestClip();
        track1.Clips.Add(clip);
        var command = new MoveClipCommand(clip, 100, track1, track2);

        // Act
        command.Redo();

        // Assert
        Assert.Empty(track1.Clips);
        Assert.Single(track2.Clips);
        Assert.Contains(clip, track2.Clips);
    }

    [Fact]
    public void MoveClipCommand_BetweenTracks_UndoRestoresOriginalTrack()
    {
        // Arrange
        var track1 = CreateTestTrack("V1");
        var track2 = CreateTestTrack("V2");
        var clip = CreateTestClip();
        track1.Clips.Add(clip);
        var command = new MoveClipCommand(clip, 100, track1, track2);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Single(track1.Clips);
        Assert.Empty(track2.Clips);
    }

    #endregion

    #region TrimClipCommand Tests

    [Fact]
    public void TrimClipCommand_Redo_TrimsClip()
    {
        // Arrange
        var clip = CreateTestClip(start: 0, end: 100);
        clip.SourceInFrame = 0;
        clip.SourceOutFrame = 100;
        var command = new TrimClipCommand(clip, 10, 90, 10, 90);

        // Act
        command.Redo();

        // Assert
        Assert.Equal(10, clip.StartFrame);
        Assert.Equal(90, clip.EndFrame);
        Assert.Equal(10, clip.SourceInFrame);
        Assert.Equal(90, clip.SourceOutFrame);
    }

    [Fact]
    public void TrimClipCommand_Undo_RestoresOriginalTrim()
    {
        // Arrange
        var clip = CreateTestClip(start: 0, end: 100);
        clip.SourceInFrame = 0;
        clip.SourceOutFrame = 100;
        var command = new TrimClipCommand(clip, 10, 90, 10, 90);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Equal(0, clip.StartFrame);
        Assert.Equal(100, clip.EndFrame);
        Assert.Equal(0, clip.SourceInFrame);
        Assert.Equal(100, clip.SourceOutFrame);
    }

    [Fact]
    public void TrimClipCommand_TrimStart_AdjustsInPoint()
    {
        // Arrange
        var clip = CreateTestClip(start: 0, end: 100);
        clip.SourceInFrame = 0;
        clip.SourceOutFrame = 100;
        var command = TrimClipCommand.TrimStart(clip, 20);

        // Act
        command.Redo();

        // Assert
        Assert.Equal(20, clip.StartFrame);
        Assert.Equal(100, clip.EndFrame);
        Assert.Equal(20, clip.SourceInFrame);
    }

    [Fact]
    public void TrimClipCommand_TrimEnd_AdjustsOutPoint()
    {
        // Arrange
        var clip = CreateTestClip(start: 0, end: 100);
        clip.SourceInFrame = 0;
        clip.SourceOutFrame = 100;
        var command = TrimClipCommand.TrimEnd(clip, 80);

        // Act
        command.Redo();

        // Assert
        Assert.Equal(0, clip.StartFrame);
        Assert.Equal(80, clip.EndFrame);
        Assert.Equal(80, clip.SourceOutFrame);
    }

    #endregion

    #region SplitClipCommand Tests

    [Fact]
    public void SplitClipCommand_Redo_SplitsClipInTwo()
    {
        // Arrange
        var track = CreateTestTrack();
        var clip = CreateTestClip(start: 0, end: 100);
        clip.SourceInFrame = 0;
        clip.SourceOutFrame = 100;
        track.Clips.Add(clip);
        var command = new SplitClipCommand(track, clip, 50);

        // Act
        command.Redo();

        // Assert
        Assert.Equal(2, track.Clips.Count);
        Assert.Equal(50, clip.EndFrame); // Original clip trimmed
        Assert.Equal(50, command.SecondClip.StartFrame);
        Assert.Equal(100, command.SecondClip.EndFrame);
    }

    [Fact]
    public void SplitClipCommand_Undo_MergesClipsBack()
    {
        // Arrange
        var track = CreateTestTrack();
        var clip = CreateTestClip(start: 0, end: 100);
        clip.SourceInFrame = 0;
        clip.SourceOutFrame = 100;
        track.Clips.Add(clip);
        var command = new SplitClipCommand(track, clip, 50);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Single(track.Clips);
        Assert.Equal(0, clip.StartFrame);
        Assert.Equal(100, clip.EndFrame);
    }

    [Fact]
    public void SplitClipCommand_SecondClip_HasCorrectName()
    {
        // Arrange
        var track = CreateTestTrack();
        var clip = CreateTestClip("My Clip", 0, 100);
        track.Clips.Add(clip);
        var command = new SplitClipCommand(track, clip, 50);

        // Act
        command.Redo();

        // Assert
        Assert.Equal("My Clip (2)", command.SecondClip.Name);
    }

    #endregion

    #region AddTransitionCommand Tests

    [Fact]
    public void AddTransitionCommand_Redo_AddsTransitionToTrack()
    {
        // Arrange
        var track = CreateTestTrack();
        var transition = new TimelineTransition { TransitionType = TransitionType.CrossDissolve };
        var command = new AddTransitionCommand(track, transition);

        // Act
        command.Redo();

        // Assert
        Assert.Single(track.Transitions);
        Assert.Contains(transition, track.Transitions);
    }

    [Fact]
    public void AddTransitionCommand_Undo_RemovesTransition()
    {
        // Arrange
        var track = CreateTestTrack();
        var transition = new TimelineTransition { TransitionType = TransitionType.Wipe };
        var command = new AddTransitionCommand(track, transition);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Empty(track.Transitions);
    }

    #endregion

    #region RemoveTransitionCommand Tests

    [Fact]
    public void RemoveTransitionCommand_Redo_RemovesTransition()
    {
        // Arrange
        var track = CreateTestTrack();
        var transition = new TimelineTransition { TransitionType = TransitionType.FadeIn };
        track.Transitions.Add(transition);
        var command = new RemoveTransitionCommand(track, transition);

        // Act
        command.Redo();

        // Assert
        Assert.Empty(track.Transitions);
    }

    [Fact]
    public void RemoveTransitionCommand_Undo_RestoresTransitionAtOriginalIndex()
    {
        // Arrange
        var track = CreateTestTrack();
        var t1 = new TimelineTransition { TransitionType = TransitionType.CrossDissolve };
        var t2 = new TimelineTransition { TransitionType = TransitionType.Wipe };
        var t3 = new TimelineTransition { TransitionType = TransitionType.FadeOut };
        track.Transitions.Add(t1);
        track.Transitions.Add(t2);
        track.Transitions.Add(t3);
        var command = new RemoveTransitionCommand(track, t2);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Equal(3, track.Transitions.Count);
        Assert.Equal(t2, track.Transitions[1]);
    }

    #endregion

    #region AddTextOverlayCommand Tests

    [Fact]
    public void AddTextOverlayCommand_Redo_AddsOverlayToTimeline()
    {
        // Arrange
        var timeline = CreateTestTimeline();
        var overlay = new TimelineTextOverlay { Text = "Hello World" };
        var command = new AddTextOverlayCommand(timeline, overlay);

        // Act
        command.Redo();

        // Assert
        Assert.Single(timeline.TextOverlays);
        Assert.Contains(overlay, timeline.TextOverlays);
    }

    [Fact]
    public void AddTextOverlayCommand_Undo_RemovesOverlay()
    {
        // Arrange
        var timeline = CreateTestTimeline();
        var overlay = new TimelineTextOverlay { Text = "Test Text" };
        var command = new AddTextOverlayCommand(timeline, overlay);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Empty(timeline.TextOverlays);
    }

    #endregion

    #region RemoveTextOverlayCommand Tests

    [Fact]
    public void RemoveTextOverlayCommand_Redo_RemovesOverlay()
    {
        // Arrange
        var timeline = CreateTestTimeline();
        var overlay = new TimelineTextOverlay { Text = "Remove Me" };
        timeline.TextOverlays.Add(overlay);
        var command = new RemoveTextOverlayCommand(timeline, overlay);

        // Act
        command.Redo();

        // Assert
        Assert.Empty(timeline.TextOverlays);
    }

    [Fact]
    public void RemoveTextOverlayCommand_Undo_RestoresOverlay()
    {
        // Arrange
        var timeline = CreateTestTimeline();
        var overlay = new TimelineTextOverlay { Text = "Restore Me" };
        timeline.TextOverlays.Add(overlay);
        var command = new RemoveTextOverlayCommand(timeline, overlay);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Single(timeline.TextOverlays);
        Assert.Contains(overlay, timeline.TextOverlays);
    }

    #endregion

    #region AddMarkerCommand Tests

    [Fact]
    public void AddMarkerCommand_Redo_AddsMarkerToTimeline()
    {
        // Arrange
        var timeline = CreateTestTimeline();
        var marker = new TimelineMarker { Name = "Chapter 1", Frame = 100 };
        var command = new AddMarkerCommand(timeline, marker);

        // Act
        command.Redo();

        // Assert
        Assert.Single(timeline.Markers);
        Assert.Contains(marker, timeline.Markers);
    }

    [Fact]
    public void AddMarkerCommand_Undo_RemovesMarker()
    {
        // Arrange
        var timeline = CreateTestTimeline();
        var marker = new TimelineMarker { Name = "Test Marker" };
        var command = new AddMarkerCommand(timeline, marker);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Empty(timeline.Markers);
    }

    [Fact]
    public void AddMarkerCommand_Description_IncludesMarkerName()
    {
        // Arrange
        var timeline = CreateTestTimeline();
        var marker = new TimelineMarker { Name = "Scene Change" };
        var command = new AddMarkerCommand(timeline, marker);

        // Assert
        Assert.Contains("Scene Change", command.Description);
    }

    #endregion

    #region RemoveMarkerCommand Tests

    [Fact]
    public void RemoveMarkerCommand_Redo_RemovesMarker()
    {
        // Arrange
        var timeline = CreateTestTimeline();
        var marker = new TimelineMarker { Name = "Delete Me" };
        timeline.Markers.Add(marker);
        var command = new RemoveMarkerCommand(timeline, marker);

        // Act
        command.Redo();

        // Assert
        Assert.Empty(timeline.Markers);
    }

    [Fact]
    public void RemoveMarkerCommand_Undo_RestoresMarkerAtOriginalIndex()
    {
        // Arrange
        var timeline = CreateTestTimeline();
        var m1 = new TimelineMarker { Name = "M1" };
        var m2 = new TimelineMarker { Name = "M2" };
        var m3 = new TimelineMarker { Name = "M3" };
        timeline.Markers.Add(m1);
        timeline.Markers.Add(m2);
        timeline.Markers.Add(m3);
        var command = new RemoveMarkerCommand(timeline, m2);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Equal(3, timeline.Markers.Count);
        Assert.Equal(m2, timeline.Markers[1]);
    }

    #endregion

    #region AddEffectCommand Tests

    [Fact]
    public void AddEffectCommand_Redo_AddsEffectToClip()
    {
        // Arrange
        var clip = CreateTestClip();
        var effect = new TimelineEffect { Name = "Blur" };
        var command = new AddEffectCommand(clip, effect);

        // Act
        command.Redo();

        // Assert
        Assert.Single(clip.Effects);
        Assert.Contains(effect, clip.Effects);
    }

    [Fact]
    public void AddEffectCommand_Undo_RemovesEffect()
    {
        // Arrange
        var clip = CreateTestClip();
        var effect = new TimelineEffect { Name = "Sharpen" };
        var command = new AddEffectCommand(clip, effect);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Empty(clip.Effects);
    }

    [Fact]
    public void AddEffectCommand_Description_IncludesEffectName()
    {
        // Arrange
        var clip = CreateTestClip();
        var effect = new TimelineEffect { Name = "Color Correction" };
        var command = new AddEffectCommand(clip, effect);

        // Assert
        Assert.Contains("Color Correction", command.Description);
    }

    #endregion

    #region RemoveEffectCommand Tests

    [Fact]
    public void RemoveEffectCommand_Redo_RemovesEffect()
    {
        // Arrange
        var clip = CreateTestClip();
        var effect = new TimelineEffect { Name = "Remove Me" };
        clip.Effects.Add(effect);
        var command = new RemoveEffectCommand(clip, effect);

        // Act
        command.Redo();

        // Assert
        Assert.Empty(clip.Effects);
    }

    [Fact]
    public void RemoveEffectCommand_Undo_RestoresEffectAtOriginalIndex()
    {
        // Arrange
        var clip = CreateTestClip();
        var e1 = new TimelineEffect { Name = "E1" };
        var e2 = new TimelineEffect { Name = "E2" };
        var e3 = new TimelineEffect { Name = "E3" };
        clip.Effects.Add(e1);
        clip.Effects.Add(e2);
        clip.Effects.Add(e3);
        var command = new RemoveEffectCommand(clip, e2);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Equal(3, clip.Effects.Count);
        Assert.Equal(e2, clip.Effects[1]);
    }

    #endregion

    #region ReorderEffectsCommand Tests

    [Fact]
    public void ReorderEffectsCommand_Redo_MovesEffectToNewIndex()
    {
        // Arrange
        var clip = CreateTestClip();
        var e1 = new TimelineEffect { Name = "E1" };
        var e2 = new TimelineEffect { Name = "E2" };
        var e3 = new TimelineEffect { Name = "E3" };
        clip.Effects.Add(e1);
        clip.Effects.Add(e2);
        clip.Effects.Add(e3);
        var command = new ReorderEffectsCommand(clip, 0, 2); // Move E1 to end

        // Act
        command.Redo();

        // Assert
        Assert.Equal(e2, clip.Effects[0]);
        Assert.Equal(e3, clip.Effects[1]);
        Assert.Equal(e1, clip.Effects[2]);
    }

    [Fact]
    public void ReorderEffectsCommand_Undo_RestoresOriginalOrder()
    {
        // Arrange
        var clip = CreateTestClip();
        var e1 = new TimelineEffect { Name = "E1" };
        var e2 = new TimelineEffect { Name = "E2" };
        var e3 = new TimelineEffect { Name = "E3" };
        clip.Effects.Add(e1);
        clip.Effects.Add(e2);
        clip.Effects.Add(e3);
        var command = new ReorderEffectsCommand(clip, 0, 2);
        command.Redo();

        // Act
        command.Undo();

        // Assert
        Assert.Equal(e1, clip.Effects[0]);
        Assert.Equal(e2, clip.Effects[1]);
        Assert.Equal(e3, clip.Effects[2]);
    }

    [Fact]
    public void ReorderEffectsCommand_Description_IsReorderEffects()
    {
        // Arrange
        var clip = CreateTestClip();
        var command = new ReorderEffectsCommand(clip, 0, 1);

        // Assert
        Assert.Equal("Reorder effects", command.Description);
    }

    #endregion
}
