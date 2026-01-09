using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VapourSynthPortable.Models;

/// <summary>
/// Represents a single keyframe at a specific frame with a value
/// </summary>
public partial class Keyframe : ObservableObject
{
    private static int _nextId = 1;

    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private long _frame;

    [ObservableProperty]
    private object? _value;

    [ObservableProperty]
    private KeyframeInterpolation _interpolation = KeyframeInterpolation.Linear;

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Bezier control point for ease-in (relative to this keyframe)
    /// </summary>
    [ObservableProperty]
    private double _easeInX = 0.25;

    [ObservableProperty]
    private double _easeInY = 0.25;

    /// <summary>
    /// Bezier control point for ease-out (relative to this keyframe)
    /// </summary>
    [ObservableProperty]
    private double _easeOutX = 0.75;

    [ObservableProperty]
    private double _easeOutY = 0.75;

    public Keyframe()
    {
        Id = _nextId++;
    }

    public Keyframe(long frame, object? value) : this()
    {
        Frame = frame;
        Value = value;
    }

    public Keyframe Clone()
    {
        return new Keyframe
        {
            Frame = Frame,
            Value = Value,
            Interpolation = Interpolation,
            EaseInX = EaseInX,
            EaseInY = EaseInY,
            EaseOutX = EaseOutX,
            EaseOutY = EaseOutY
        };
    }
}

/// <summary>
/// A collection of keyframes for a single animatable property
/// </summary>
public partial class KeyframeTrack : ObservableObject
{
    private static int _nextId = 1;

    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _parameterName = "";

    [ObservableProperty]
    private string _displayName = "";

    [ObservableProperty]
    private ObservableCollection<Keyframe> _keyframes = [];

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _isEnabled = true;

    /// <summary>
    /// The effect parameter this track animates
    /// </summary>
    public EffectParameter? Parameter { get; set; }

    /// <summary>
    /// The effect this track belongs to
    /// </summary>
    public TimelineEffect? Effect { get; set; }

    public bool HasKeyframes => Keyframes.Count > 0;

    public KeyframeTrack()
    {
        Id = _nextId++;
    }

    /// <summary>
    /// Add a keyframe at the specified frame
    /// </summary>
    public Keyframe AddKeyframe(long frame, object? value, KeyframeInterpolation interpolation = KeyframeInterpolation.Linear)
    {
        // Check if keyframe already exists at this frame
        var existing = Keyframes.FirstOrDefault(k => k.Frame == frame);
        if (existing != null)
        {
            existing.Value = value;
            existing.Interpolation = interpolation;
            return existing;
        }

        var keyframe = new Keyframe(frame, value) { Interpolation = interpolation };

        // Insert in sorted order
        var index = 0;
        while (index < Keyframes.Count && Keyframes[index].Frame < frame)
        {
            index++;
        }
        Keyframes.Insert(index, keyframe);

        OnPropertyChanged(nameof(HasKeyframes));
        return keyframe;
    }

    /// <summary>
    /// Remove a keyframe
    /// </summary>
    public void RemoveKeyframe(Keyframe keyframe)
    {
        Keyframes.Remove(keyframe);
        OnPropertyChanged(nameof(HasKeyframes));
    }

    /// <summary>
    /// Remove keyframe at specified frame
    /// </summary>
    public void RemoveKeyframeAt(long frame)
    {
        var keyframe = Keyframes.FirstOrDefault(k => k.Frame == frame);
        if (keyframe != null)
        {
            Keyframes.Remove(keyframe);
            OnPropertyChanged(nameof(HasKeyframes));
        }
    }

    /// <summary>
    /// Get the interpolated value at the specified frame
    /// </summary>
    public object? GetValueAtFrame(long frame)
    {
        if (Keyframes.Count == 0)
            return Parameter?.Value;

        if (Keyframes.Count == 1)
            return Keyframes[0].Value;

        // Find surrounding keyframes
        Keyframe? before = null;
        Keyframe? after = null;

        foreach (var kf in Keyframes)
        {
            if (kf.Frame <= frame)
                before = kf;
            else if (kf.Frame > frame && after == null)
                after = kf;
        }

        // If before first keyframe, return first value
        if (before == null)
            return Keyframes[0].Value;

        // If after last keyframe, return last value
        if (after == null)
            return before.Value;

        // Interpolate between keyframes
        return Interpolate(before, after, frame);
    }

    private object? Interpolate(Keyframe from, Keyframe to, long frame)
    {
        var totalFrames = to.Frame - from.Frame;
        if (totalFrames <= 0) return from.Value;

        var progress = (double)(frame - from.Frame) / totalFrames;

        // Apply easing based on interpolation type
        var easedProgress = from.Interpolation switch
        {
            KeyframeInterpolation.Hold => 0.0,
            KeyframeInterpolation.Linear => progress,
            KeyframeInterpolation.EaseIn => EaseIn(progress),
            KeyframeInterpolation.EaseOut => EaseOut(progress),
            KeyframeInterpolation.EaseInOut => EaseInOut(progress),
            KeyframeInterpolation.Bezier => BezierEase(progress, from.EaseOutX, from.EaseOutY, to.EaseInX, to.EaseInY),
            _ => progress
        };

        // Interpolate based on value type
        return InterpolateValue(from.Value, to.Value, easedProgress);
    }

    private static object? InterpolateValue(object? fromValue, object? toValue, double t)
    {
        if (fromValue == null || toValue == null)
            return t < 0.5 ? fromValue : toValue;

        // Handle different value types
        return (fromValue, toValue) switch
        {
            (double fromD, double toD) => fromD + (toD - fromD) * t,
            (float fromF, float toF) => fromF + (toF - fromF) * (float)t,
            (int fromI, int toI) => (int)(fromI + (toI - fromI) * t),
            (long fromL, long toL) => (long)(fromL + (toL - fromL) * t),
            (decimal fromDec, decimal toDec) => fromDec + (toDec - fromDec) * (decimal)t,
            _ => t < 0.5 ? fromValue : toValue
        };
    }

    private static double EaseIn(double t) => t * t;
    private static double EaseOut(double t) => 1 - (1 - t) * (1 - t);
    private static double EaseInOut(double t) => t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;

    private static double BezierEase(double t, double x1, double y1, double x2, double y2)
    {
        // Simplified cubic bezier approximation
        var cx = 3 * x1;
        var bx = 3 * (x2 - x1) - cx;
        var ax = 1 - cx - bx;

        var cy = 3 * y1;
        var by = 3 * (y2 - y1) - cy;
        var ay = 1 - cy - by;

        // Solve for t given x using Newton-Raphson
        var tGuess = t;
        for (int i = 0; i < 8; i++)
        {
            var xGuess = ((ax * tGuess + bx) * tGuess + cx) * tGuess;
            var xSlope = (3 * ax * tGuess + 2 * bx) * tGuess + cx;
            if (Math.Abs(xSlope) < 0.000001) break;
            tGuess -= (xGuess - t) / xSlope;
        }

        return ((ay * tGuess + by) * tGuess + cy) * tGuess;
    }

    /// <summary>
    /// Get the keyframe at or before the specified frame
    /// </summary>
    public Keyframe? GetKeyframeAtOrBefore(long frame)
    {
        return Keyframes.LastOrDefault(k => k.Frame <= frame);
    }

    /// <summary>
    /// Get the keyframe after the specified frame
    /// </summary>
    public Keyframe? GetKeyframeAfter(long frame)
    {
        return Keyframes.FirstOrDefault(k => k.Frame > frame);
    }

    /// <summary>
    /// Check if there's a keyframe at the exact frame
    /// </summary>
    public bool HasKeyframeAt(long frame)
    {
        return Keyframes.Any(k => k.Frame == frame);
    }

    public KeyframeTrack Clone()
    {
        var clone = new KeyframeTrack
        {
            ParameterName = ParameterName,
            DisplayName = DisplayName,
            IsExpanded = IsExpanded,
            IsEnabled = IsEnabled
        };

        foreach (var kf in Keyframes)
        {
            clone.Keyframes.Add(kf.Clone());
        }

        return clone;
    }
}

/// <summary>
/// Interpolation types for keyframes
/// </summary>
public enum KeyframeInterpolation
{
    /// <summary>
    /// Hold the value until the next keyframe (no interpolation)
    /// </summary>
    Hold,

    /// <summary>
    /// Linear interpolation between keyframes
    /// </summary>
    Linear,

    /// <summary>
    /// Ease in (slow start)
    /// </summary>
    EaseIn,

    /// <summary>
    /// Ease out (slow end)
    /// </summary>
    EaseOut,

    /// <summary>
    /// Ease in and out (slow start and end)
    /// </summary>
    EaseInOut,

    /// <summary>
    /// Custom bezier curve
    /// </summary>
    Bezier
}
