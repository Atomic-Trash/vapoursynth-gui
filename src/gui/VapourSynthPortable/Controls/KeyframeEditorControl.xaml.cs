using System.Windows;
using System.Windows.Controls;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Controls;

public partial class KeyframeEditorControl : UserControl
{
    public static readonly DependencyProperty SelectedEffectProperty =
        DependencyProperty.Register(
            nameof(SelectedEffect),
            typeof(TimelineEffect),
            typeof(KeyframeEditorControl),
            new PropertyMetadata(null, OnSelectedEffectChanged));

    public static readonly DependencyProperty SelectedParameterProperty =
        DependencyProperty.Register(
            nameof(SelectedParameter),
            typeof(EffectParameter),
            typeof(KeyframeEditorControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CurrentFrameProperty =
        DependencyProperty.Register(
            nameof(CurrentFrame),
            typeof(long),
            typeof(KeyframeEditorControl),
            new PropertyMetadata(0L, OnCurrentFrameChanged));

    public TimelineEffect? SelectedEffect
    {
        get => (TimelineEffect?)GetValue(SelectedEffectProperty);
        set => SetValue(SelectedEffectProperty, value);
    }

    public EffectParameter? SelectedParameter
    {
        get => (EffectParameter?)GetValue(SelectedParameterProperty);
        set => SetValue(SelectedParameterProperty, value);
    }

    public long CurrentFrame
    {
        get => (long)GetValue(CurrentFrameProperty);
        set => SetValue(CurrentFrameProperty, value);
    }

    public KeyframeEditorControl()
    {
        InitializeComponent();
        UpdateUI();
    }

    private static void OnSelectedEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KeyframeEditorControl control)
        {
            control.UpdateUI();
        }
    }

    private static void OnCurrentFrameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KeyframeEditorControl control)
        {
            control.UpdateFrameInfo();
        }
    }

    private void UpdateUI()
    {
        if (SelectedEffect == null || !SelectedEffect.HasKeyframes)
        {
            TracksControl.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Visible;
            KeyframeCount.Text = "";
        }
        else
        {
            TracksControl.Visibility = Visibility.Visible;
            EmptyState.Visibility = Visibility.Collapsed;
            TracksControl.ItemsSource = SelectedEffect.KeyframeTracks;

            var totalKeyframes = SelectedEffect.KeyframeTracks.Sum(t => t.Keyframes.Count);
            KeyframeCount.Text = $"({totalKeyframes})";
        }

        UpdateFrameInfo();
    }

    private void UpdateFrameInfo()
    {
        if (SelectedEffect == null || SelectedParameter == null)
        {
            FrameInfo.Text = $"Frame: {CurrentFrame}";
        }
        else
        {
            var track = SelectedEffect.GetKeyframeTrack(SelectedParameter.Name);
            var hasKeyframe = track?.HasKeyframeAt(CurrentFrame) ?? false;
            FrameInfo.Text = hasKeyframe
                ? $"Frame: {CurrentFrame} (keyframe)"
                : $"Frame: {CurrentFrame}";
        }
    }

    private EditViewModel? GetEditViewModel()
    {
        var parent = Parent;
        while (parent != null)
        {
            if (parent is FrameworkElement element && element.DataContext is EditViewModel vm)
                return vm;
            parent = (parent as FrameworkElement)?.Parent;
        }
        return null;
    }

    private void AddKeyframe_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedEffect == null || SelectedParameter == null)
        {
            ToastService.Instance.ShowInfo("Select an effect parameter first");
            return;
        }

        var value = SelectedParameter.Value;
        var keyframe = SelectedEffect.AddKeyframe(SelectedParameter, CurrentFrame, value, GetSelectedInterpolation());

        ToastService.Instance.ShowSuccess($"Keyframe added at frame {CurrentFrame}");
        UpdateUI();
    }

    private void RemoveKeyframe_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedEffect == null || SelectedParameter == null)
        {
            ToastService.Instance.ShowInfo("Select an effect parameter first");
            return;
        }

        var track = SelectedEffect.GetKeyframeTrack(SelectedParameter.Name);
        if (track == null || !track.HasKeyframeAt(CurrentFrame))
        {
            ToastService.Instance.ShowInfo("No keyframe at current frame");
            return;
        }

        track.RemoveKeyframeAt(CurrentFrame);
        ToastService.Instance.ShowSuccess($"Keyframe removed at frame {CurrentFrame}");
        UpdateUI();
    }

    private void PreviousKeyframe_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedEffect == null) return;

        // Find the previous keyframe across all tracks
        long? prevFrame = null;

        foreach (var track in SelectedEffect.KeyframeTracks)
        {
            var kf = track.GetKeyframeAtOrBefore(CurrentFrame - 1);
            if (kf != null && (prevFrame == null || kf.Frame > prevFrame))
            {
                prevFrame = kf.Frame;
            }
        }

        if (prevFrame.HasValue)
        {
            var vm = GetEditViewModel();
            if (vm != null)
            {
                vm.Timeline.PlayheadFrame = prevFrame.Value;
            }
        }
        else
        {
            ToastService.Instance.ShowInfo("No previous keyframe");
        }
    }

    private void NextKeyframe_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedEffect == null) return;

        // Find the next keyframe across all tracks
        long? nextFrame = null;

        foreach (var track in SelectedEffect.KeyframeTracks)
        {
            var kf = track.GetKeyframeAfter(CurrentFrame);
            if (kf != null && (nextFrame == null || kf.Frame < nextFrame))
            {
                nextFrame = kf.Frame;
            }
        }

        if (nextFrame.HasValue)
        {
            var vm = GetEditViewModel();
            if (vm != null)
            {
                vm.Timeline.PlayheadFrame = nextFrame.Value;
            }
        }
        else
        {
            ToastService.Instance.ShowInfo("No next keyframe");
        }
    }

    private void InterpolationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedEffect == null || SelectedParameter == null) return;

        var track = SelectedEffect.GetKeyframeTrack(SelectedParameter.Name);
        var keyframe = track?.Keyframes.FirstOrDefault(k => k.Frame == CurrentFrame);

        if (keyframe != null)
        {
            keyframe.Interpolation = GetSelectedInterpolation();
        }
    }

    private KeyframeInterpolation GetSelectedInterpolation()
    {
        return InterpolationCombo.SelectedIndex switch
        {
            0 => KeyframeInterpolation.Hold,
            1 => KeyframeInterpolation.Linear,
            2 => KeyframeInterpolation.EaseIn,
            3 => KeyframeInterpolation.EaseOut,
            4 => KeyframeInterpolation.EaseInOut,
            _ => KeyframeInterpolation.Linear
        };
    }
}
