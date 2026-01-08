using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VapourSynthPortable.Models;

/// <summary>
/// Represents a text overlay on the timeline
/// </summary>
public partial class TimelineTextOverlay : ObservableObject
{
    private static int _nextId = 1;

    public TimelineTextOverlay()
    {
        Id = _nextId++;
    }

    public int Id { get; set; }

    [ObservableProperty]
    private string _text = "Text";

    [ObservableProperty]
    private long _startFrame;

    [ObservableProperty]
    private long _durationFrames = 72; // Default 3 seconds at 24fps

    [ObservableProperty]
    private string _fontFamily = "Segoe UI";

    [ObservableProperty]
    private double _fontSize = 48;

    [ObservableProperty]
    private Color _fontColor = Colors.White;

    [ObservableProperty]
    private Color _backgroundColor = Colors.Transparent;

    [ObservableProperty]
    private double _opacity = 1.0;

    [ObservableProperty]
    private double _x = 0.5; // 0-1 normalized, 0.5 = center

    [ObservableProperty]
    private double _y = 0.9; // 0-1 normalized, 0.9 = near bottom

    [ObservableProperty]
    private TextAlignment _alignment = TextAlignment.Center;

    [ObservableProperty]
    private bool _hasShadow = true;

    [ObservableProperty]
    private Color _shadowColor = Colors.Black;

    [ObservableProperty]
    private double _shadowOffsetX = 2;

    [ObservableProperty]
    private double _shadowOffsetY = 2;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private TextAnimationType _animationIn = TextAnimationType.None;

    [ObservableProperty]
    private TextAnimationType _animationOut = TextAnimationType.None;

    [ObservableProperty]
    private int _animationDurationFrames = 12; // 0.5 seconds at 24fps

    public long EndFrame => StartFrame + DurationFrames;

    /// <summary>
    /// Creates a deep copy of this text overlay
    /// </summary>
    public TimelineTextOverlay Clone()
    {
        return new TimelineTextOverlay
        {
            Id = Id,
            Text = Text,
            StartFrame = StartFrame,
            DurationFrames = DurationFrames,
            FontFamily = FontFamily,
            FontSize = FontSize,
            FontColor = FontColor,
            BackgroundColor = BackgroundColor,
            Opacity = Opacity,
            X = X,
            Y = Y,
            Alignment = Alignment,
            HasShadow = HasShadow,
            ShadowColor = ShadowColor,
            ShadowOffsetX = ShadowOffsetX,
            ShadowOffsetY = ShadowOffsetY,
            AnimationIn = AnimationIn,
            AnimationOut = AnimationOut,
            AnimationDurationFrames = AnimationDurationFrames
        };
    }
}

/// <summary>
/// Animation types for text overlays
/// </summary>
public enum TextAnimationType
{
    None,
    FadeIn,
    FadeOut,
    SlideFromLeft,
    SlideFromRight,
    SlideFromTop,
    SlideFromBottom,
    ZoomIn,
    ZoomOut,
    Typewriter
}

/// <summary>
/// Serializable data for text overlay in project files
/// </summary>
public class TextOverlayData
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public long StartFrame { get; set; }
    public long DurationFrames { get; set; }
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 48;
    public string FontColorHex { get; set; } = "#FFFFFF";
    public string BackgroundColorHex { get; set; } = "#00000000";
    public double Opacity { get; set; } = 1.0;
    public double X { get; set; } = 0.5;
    public double Y { get; set; } = 0.9;
    public int Alignment { get; set; }
    public bool HasShadow { get; set; } = true;
    public string ShadowColorHex { get; set; } = "#000000";
    public double ShadowOffsetX { get; set; } = 2;
    public double ShadowOffsetY { get; set; } = 2;
    public int AnimationIn { get; set; }
    public int AnimationOut { get; set; }
    public int AnimationDurationFrames { get; set; } = 12;
}
