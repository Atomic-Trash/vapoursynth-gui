using System.Windows;
using System.Windows.Controls;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Controls;

public partial class TransitionControl : UserControl
{
    public static readonly DependencyProperty TransitionProperty =
        DependencyProperty.Register(nameof(Transition), typeof(TimelineTransition), typeof(TransitionControl),
            new PropertyMetadata(null, OnTransitionChanged));

    public TimelineTransition? Transition
    {
        get => (TimelineTransition?)GetValue(TransitionProperty);
        set => SetValue(TransitionProperty, value);
    }

    public TransitionControl()
    {
        InitializeComponent();
    }

    private static void OnTransitionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TransitionControl control && e.NewValue is TimelineTransition transition)
        {
            control.DataContext = transition;
        }
    }
}
