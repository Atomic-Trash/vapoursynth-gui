using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services.Commands;

/// <summary>
/// Command to add a clip to the timeline
/// </summary>
public class AddClipCommand : IUndoAction
{
    private readonly TimelineTrack _track;
    private readonly TimelineClip _clip;
    private readonly int _index;

    public string Description { get; }

    public AddClipCommand(TimelineTrack track, TimelineClip clip, int index = -1)
    {
        _track = track;
        _clip = clip;
        _index = index >= 0 ? index : track.Clips.Count;
        Description = $"Add clip: {clip.Name}";
    }

    public void Undo()
    {
        _track.Clips.Remove(_clip);
    }

    public void Redo()
    {
        if (_index <= _track.Clips.Count)
            _track.Clips.Insert(_index, _clip);
        else
            _track.Clips.Add(_clip);
    }
}

/// <summary>
/// Command to remove a clip from the timeline
/// </summary>
public class RemoveClipCommand : IUndoAction
{
    private readonly TimelineTrack _track;
    private readonly TimelineClip _clip;
    private readonly int _index;

    public string Description { get; }

    public RemoveClipCommand(TimelineTrack track, TimelineClip clip)
    {
        _track = track;
        _clip = clip;
        _index = track.Clips.IndexOf(clip);
        Description = $"Remove clip: {clip.Name}";
    }

    public void Undo()
    {
        if (_index >= 0 && _index <= _track.Clips.Count)
            _track.Clips.Insert(_index, _clip);
        else
            _track.Clips.Add(_clip);
    }

    public void Redo()
    {
        _track.Clips.Remove(_clip);
    }
}

/// <summary>
/// Command to move a clip to a new position on the timeline
/// </summary>
public class MoveClipCommand : IUndoAction
{
    private readonly TimelineClip _clip;
    private readonly long _oldStartFrame;
    private readonly long _oldEndFrame;
    private readonly long _newStartFrame;
    private readonly long _newEndFrame;
    private readonly TimelineTrack? _oldTrack;
    private readonly TimelineTrack? _newTrack;

    public string Description { get; }

    public MoveClipCommand(
        TimelineClip clip,
        long newStartFrame,
        TimelineTrack? oldTrack = null,
        TimelineTrack? newTrack = null)
    {
        _clip = clip;
        _oldStartFrame = clip.StartFrame;
        _oldEndFrame = clip.EndFrame;
        var duration = clip.EndFrame - clip.StartFrame;
        _newStartFrame = newStartFrame;
        _newEndFrame = newStartFrame + duration;
        _oldTrack = oldTrack;
        _newTrack = newTrack;
        Description = $"Move clip: {clip.Name}";
    }

    public void Undo()
    {
        // Move between tracks if applicable
        if (_oldTrack != null && _newTrack != null && _oldTrack != _newTrack)
        {
            _newTrack.Clips.Remove(_clip);
            _oldTrack.Clips.Add(_clip);
        }

        _clip.StartFrame = _oldStartFrame;
        _clip.EndFrame = _oldEndFrame;
    }

    public void Redo()
    {
        // Move between tracks if applicable
        if (_oldTrack != null && _newTrack != null && _oldTrack != _newTrack)
        {
            _oldTrack.Clips.Remove(_clip);
            _newTrack.Clips.Add(_clip);
        }

        _clip.StartFrame = _newStartFrame;
        _clip.EndFrame = _newEndFrame;
    }
}

/// <summary>
/// Command to trim a clip (change in/out points)
/// </summary>
public class TrimClipCommand : IUndoAction
{
    private readonly TimelineClip _clip;
    private readonly long _oldStartFrame;
    private readonly long _oldEndFrame;
    private readonly long _oldSourceInFrame;
    private readonly long _oldSourceOutFrame;
    private readonly long _newStartFrame;
    private readonly long _newEndFrame;
    private readonly long _newSourceInFrame;
    private readonly long _newSourceOutFrame;

    public string Description { get; }

    public TrimClipCommand(
        TimelineClip clip,
        long newStartFrame,
        long newEndFrame,
        long newSourceInFrame,
        long newSourceOutFrame)
    {
        _clip = clip;
        _oldStartFrame = clip.StartFrame;
        _oldEndFrame = clip.EndFrame;
        _oldSourceInFrame = clip.SourceInFrame;
        _oldSourceOutFrame = clip.SourceOutFrame;
        _newStartFrame = newStartFrame;
        _newEndFrame = newEndFrame;
        _newSourceInFrame = newSourceInFrame;
        _newSourceOutFrame = newSourceOutFrame;
        Description = $"Trim clip: {clip.Name}";
    }

    /// <summary>
    /// Creates a trim command for left-side trim (trim in-point)
    /// </summary>
    public static TrimClipCommand TrimStart(TimelineClip clip, long newStartFrame)
    {
        var frameDelta = newStartFrame - clip.StartFrame;
        return new TrimClipCommand(
            clip,
            newStartFrame,
            clip.EndFrame,
            clip.SourceInFrame + frameDelta,
            clip.SourceOutFrame);
    }

    /// <summary>
    /// Creates a trim command for right-side trim (trim out-point)
    /// </summary>
    public static TrimClipCommand TrimEnd(TimelineClip clip, long newEndFrame)
    {
        var frameDelta = newEndFrame - clip.EndFrame;
        return new TrimClipCommand(
            clip,
            clip.StartFrame,
            newEndFrame,
            clip.SourceInFrame,
            clip.SourceOutFrame + frameDelta);
    }

    public void Undo()
    {
        _clip.StartFrame = _oldStartFrame;
        _clip.EndFrame = _oldEndFrame;
        _clip.SourceInFrame = _oldSourceInFrame;
        _clip.SourceOutFrame = _oldSourceOutFrame;
    }

    public void Redo()
    {
        _clip.StartFrame = _newStartFrame;
        _clip.EndFrame = _newEndFrame;
        _clip.SourceInFrame = _newSourceInFrame;
        _clip.SourceOutFrame = _newSourceOutFrame;
    }
}

/// <summary>
/// Command to split a clip at a specific frame
/// </summary>
public class SplitClipCommand : IUndoAction
{
    private readonly TimelineTrack _track;
    private readonly TimelineClip _originalClip;
    private readonly TimelineClip _secondClip;
    private readonly long _originalEndFrame;
    private readonly long _originalSourceOutFrame;
    private readonly long _splitFrame;

    public string Description { get; }

    public SplitClipCommand(TimelineTrack track, TimelineClip clipToSplit, long splitFrame)
    {
        _track = track;
        _originalClip = clipToSplit;
        _splitFrame = splitFrame;
        _originalEndFrame = clipToSplit.EndFrame;
        _originalSourceOutFrame = clipToSplit.SourceOutFrame;

        // Calculate the source frame at the split point
        var splitSourceFrame = clipToSplit.SourceInFrame +
            (splitFrame - clipToSplit.StartFrame);

        // Create the second clip (right side of split)
        _secondClip = clipToSplit.Clone();
        _secondClip.StartFrame = splitFrame;
        _secondClip.EndFrame = _originalEndFrame;
        _secondClip.SourceInFrame = splitSourceFrame;
        _secondClip.SourceOutFrame = _originalSourceOutFrame;
        _secondClip.Name = $"{clipToSplit.Name} (2)";

        Description = $"Split clip: {clipToSplit.Name}";
    }

    public void Undo()
    {
        // Remove the second clip
        _track.Clips.Remove(_secondClip);

        // Restore original clip's end point
        _originalClip.EndFrame = _originalEndFrame;
        _originalClip.SourceOutFrame = _originalSourceOutFrame;
    }

    public void Redo()
    {
        // Trim original clip to split point
        var splitSourceFrame = _originalClip.SourceInFrame +
            (_splitFrame - _originalClip.StartFrame);
        _originalClip.EndFrame = _splitFrame;
        _originalClip.SourceOutFrame = splitSourceFrame;

        // Add the second clip
        _track.Clips.Add(_secondClip);
    }

    /// <summary>
    /// Gets the clip created by the split (the right portion)
    /// </summary>
    public TimelineClip SecondClip => _secondClip;
}

/// <summary>
/// Command to add a transition between clips
/// </summary>
public class AddTransitionCommand : IUndoAction
{
    private readonly TimelineTrack _track;
    private readonly TimelineTransition _transition;

    public string Description { get; }

    public AddTransitionCommand(TimelineTrack track, TimelineTransition transition)
    {
        _track = track;
        _transition = transition;
        Description = $"Add transition: {transition.DisplayName}";
    }

    public void Undo()
    {
        _track.Transitions.Remove(_transition);
    }

    public void Redo()
    {
        _track.Transitions.Add(_transition);
    }
}

/// <summary>
/// Command to remove a transition
/// </summary>
public class RemoveTransitionCommand : IUndoAction
{
    private readonly TimelineTrack _track;
    private readonly TimelineTransition _transition;
    private readonly int _index;

    public string Description { get; }

    public RemoveTransitionCommand(TimelineTrack track, TimelineTransition transition)
    {
        _track = track;
        _transition = transition;
        _index = track.Transitions.IndexOf(transition);
        Description = $"Remove transition: {transition.DisplayName}";
    }

    public void Undo()
    {
        if (_index >= 0 && _index <= _track.Transitions.Count)
            _track.Transitions.Insert(_index, _transition);
        else
            _track.Transitions.Add(_transition);
    }

    public void Redo()
    {
        _track.Transitions.Remove(_transition);
    }
}

/// <summary>
/// Command to add a text overlay to the timeline
/// </summary>
public class AddTextOverlayCommand : IUndoAction
{
    private readonly Timeline _timeline;
    private readonly TimelineTextOverlay _overlay;

    public string Description { get; }

    public AddTextOverlayCommand(Timeline timeline, TimelineTextOverlay overlay)
    {
        _timeline = timeline;
        _overlay = overlay;
        Description = $"Add text overlay: {overlay.Text.Substring(0, Math.Min(20, overlay.Text.Length))}...";
    }

    public void Undo()
    {
        _timeline.TextOverlays.Remove(_overlay);
    }

    public void Redo()
    {
        _timeline.TextOverlays.Add(_overlay);
    }
}

/// <summary>
/// Command to remove a text overlay from the timeline
/// </summary>
public class RemoveTextOverlayCommand : IUndoAction
{
    private readonly Timeline _timeline;
    private readonly TimelineTextOverlay _overlay;
    private readonly int _index;

    public string Description { get; }

    public RemoveTextOverlayCommand(Timeline timeline, TimelineTextOverlay overlay)
    {
        _timeline = timeline;
        _overlay = overlay;
        _index = timeline.TextOverlays.IndexOf(overlay);
        Description = $"Remove text overlay";
    }

    public void Undo()
    {
        if (_index >= 0 && _index <= _timeline.TextOverlays.Count)
            _timeline.TextOverlays.Insert(_index, _overlay);
        else
            _timeline.TextOverlays.Add(_overlay);
    }

    public void Redo()
    {
        _timeline.TextOverlays.Remove(_overlay);
    }
}

/// <summary>
/// Command to add a marker to the timeline
/// </summary>
public class AddMarkerCommand : IUndoAction
{
    private readonly Timeline _timeline;
    private readonly TimelineMarker _marker;

    public string Description { get; }

    public AddMarkerCommand(Timeline timeline, TimelineMarker marker)
    {
        _timeline = timeline;
        _marker = marker;
        Description = $"Add marker: {marker.Name}";
    }

    public void Undo()
    {
        _timeline.Markers.Remove(_marker);
    }

    public void Redo()
    {
        _timeline.Markers.Add(_marker);
    }
}

/// <summary>
/// Command to remove a marker from the timeline
/// </summary>
public class RemoveMarkerCommand : IUndoAction
{
    private readonly Timeline _timeline;
    private readonly TimelineMarker _marker;
    private readonly int _index;

    public string Description { get; }

    public RemoveMarkerCommand(Timeline timeline, TimelineMarker marker)
    {
        _timeline = timeline;
        _marker = marker;
        _index = timeline.Markers.IndexOf(marker);
        Description = $"Remove marker: {marker.Name}";
    }

    public void Undo()
    {
        if (_index >= 0 && _index <= _timeline.Markers.Count)
            _timeline.Markers.Insert(_index, _marker);
        else
            _timeline.Markers.Add(_marker);
    }

    public void Redo()
    {
        _timeline.Markers.Remove(_marker);
    }
}

/// <summary>
/// Command to add an effect to a clip
/// </summary>
public class AddEffectCommand : IUndoAction
{
    private readonly TimelineClip _clip;
    private readonly TimelineEffect _effect;

    public string Description { get; }

    public AddEffectCommand(TimelineClip clip, TimelineEffect effect)
    {
        _clip = clip;
        _effect = effect;
        Description = $"Add effect: {effect.Name}";
    }

    public void Undo()
    {
        _clip.Effects.Remove(_effect);
    }

    public void Redo()
    {
        _clip.Effects.Add(_effect);
    }
}

/// <summary>
/// Command to remove an effect from a clip
/// </summary>
public class RemoveEffectCommand : IUndoAction
{
    private readonly TimelineClip _clip;
    private readonly TimelineEffect _effect;
    private readonly int _index;

    public string Description { get; }

    public RemoveEffectCommand(TimelineClip clip, TimelineEffect effect)
    {
        _clip = clip;
        _effect = effect;
        _index = clip.Effects.IndexOf(effect);
        Description = $"Remove effect: {effect.Name}";
    }

    public void Undo()
    {
        if (_index >= 0 && _index <= _clip.Effects.Count)
            _clip.Effects.Insert(_index, _effect);
        else
            _clip.Effects.Add(_effect);
    }

    public void Redo()
    {
        _clip.Effects.Remove(_effect);
    }
}

/// <summary>
/// Command to reorder effects on a clip
/// </summary>
public class ReorderEffectsCommand : IUndoAction
{
    private readonly TimelineClip _clip;
    private readonly int _oldIndex;
    private readonly int _newIndex;

    public string Description { get; }

    public ReorderEffectsCommand(TimelineClip clip, int oldIndex, int newIndex)
    {
        _clip = clip;
        _oldIndex = oldIndex;
        _newIndex = newIndex;
        Description = "Reorder effects";
    }

    public void Undo()
    {
        _clip.Effects.Move(_newIndex, _oldIndex);
    }

    public void Redo()
    {
        _clip.Effects.Move(_oldIndex, _newIndex);
    }
}
