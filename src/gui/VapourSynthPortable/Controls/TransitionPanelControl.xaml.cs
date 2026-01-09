using System.Windows;
using System.Windows.Controls;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Controls;

public partial class TransitionPanelControl : UserControl
{
    public static readonly DependencyProperty TransitionDurationProperty =
        DependencyProperty.Register(
            nameof(TransitionDuration),
            typeof(int),
            typeof(TransitionPanelControl),
            new PropertyMetadata(24));

    public int TransitionDuration
    {
        get => (int)GetValue(TransitionDurationProperty);
        set => SetValue(TransitionDurationProperty, value);
    }

    public TransitionPanelControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    private EditViewModel? GetEditViewModel()
    {
        // Walk up the visual tree to find the EditPage's DataContext
        var parent = Parent;
        while (parent != null)
        {
            if (parent is FrameworkElement element && element.DataContext is EditViewModel vm)
                return vm;
            parent = (parent as FrameworkElement)?.Parent;
        }
        return null;
    }

    private void ApplyTransition_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string transitionTag)
            return;

        var vm = GetEditViewModel();
        if (vm == null)
        {
            ToastService.Instance.ShowWarning("Cannot apply transition", "Edit view not available");
            return;
        }

        // Parse the transition type from the tag
        var preset = ParseTransitionPreset(transitionTag);
        if (preset == null)
        {
            ToastService.Instance.ShowError("Unknown transition type");
            return;
        }

        // Set the preset and duration
        preset.DefaultDuration = TransitionDuration;
        vm.SelectedTransitionPreset = preset;

        // Apply the transition
        if (vm.AddTransitionCommand.CanExecute(null))
        {
            vm.AddTransitionCommand.Execute(null);
        }
        else
        {
            ToastService.Instance.ShowInfo("Select a clip to apply transition");
        }
    }

    private void RemoveTransition_Click(object sender, RoutedEventArgs e)
    {
        var vm = GetEditViewModel();
        if (vm == null)
            return;

        if (vm.RemoveTransitionCommand.CanExecute(null))
        {
            vm.RemoveTransitionCommand.Execute(null);
        }
        else
        {
            ToastService.Instance.ShowInfo("No transition to remove");
        }
    }

    private TransitionPreset? ParseTransitionPreset(string tag)
    {
        return tag switch
        {
            "CrossDissolve" => new TransitionPreset { Name = "Cross Dissolve", Type = TransitionType.CrossDissolve },
            "DipToBlack" => new TransitionPreset { Name = "Dip to Black", Type = TransitionType.DipToBlack },
            "DipToWhite" => new TransitionPreset { Name = "Dip to White", Type = TransitionType.DipToWhite },
            "FadeIn" => new TransitionPreset { Name = "Fade In", Type = TransitionType.FadeIn },
            "FadeOut" => new TransitionPreset { Name = "Fade Out", Type = TransitionType.FadeOut },
            "WipeLeft" => new TransitionPreset { Name = "Wipe Left", Type = TransitionType.Wipe, Direction = WipeDirection.Left },
            "WipeRight" => new TransitionPreset { Name = "Wipe Right", Type = TransitionType.Wipe, Direction = WipeDirection.Right },
            "WipeUp" => new TransitionPreset { Name = "Wipe Up", Type = TransitionType.Wipe, Direction = WipeDirection.Up },
            "WipeDown" => new TransitionPreset { Name = "Wipe Down", Type = TransitionType.Wipe, Direction = WipeDirection.Down },
            "SlideLeft" => new TransitionPreset { Name = "Slide Left", Type = TransitionType.Slide, Direction = WipeDirection.Left },
            "PushRight" => new TransitionPreset { Name = "Push Right", Type = TransitionType.Push, Direction = WipeDirection.Right },
            "Cut" => new TransitionPreset { Name = "Cut", Type = TransitionType.Cut, DefaultDuration = 0 },
            _ => null
        };
    }
}
