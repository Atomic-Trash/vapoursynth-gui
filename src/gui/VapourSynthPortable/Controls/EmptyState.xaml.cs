using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VapourSynthPortable.Controls;

/// <summary>
/// Reusable empty state control with icon, title, subtitle, and optional action button.
/// </summary>
public partial class EmptyState : UserControl
{
    public EmptyState()
    {
        InitializeComponent();
    }

    #region Dependency Properties

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(string),
            typeof(EmptyState),
            new PropertyMetadata("\uE8B7")); // Default: folder icon

    /// <summary>
    /// Segoe MDL2 Assets icon character
    /// </summary>
    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(EmptyState),
            new PropertyMetadata("No items"));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(
            nameof(Subtitle),
            typeof(string),
            typeof(EmptyState),
            new PropertyMetadata(null));

    public string? Subtitle
    {
        get => (string?)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public static readonly DependencyProperty ActionTextProperty =
        DependencyProperty.Register(
            nameof(ActionText),
            typeof(string),
            typeof(EmptyState),
            new PropertyMetadata("Add Item"));

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public static readonly DependencyProperty ActionCommandProperty =
        DependencyProperty.Register(
            nameof(ActionCommand),
            typeof(ICommand),
            typeof(EmptyState),
            new PropertyMetadata(null));

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public static readonly DependencyProperty ShowActionProperty =
        DependencyProperty.Register(
            nameof(ShowAction),
            typeof(bool),
            typeof(EmptyState),
            new PropertyMetadata(true));

    public bool ShowAction
    {
        get => (bool)GetValue(ShowActionProperty);
        set => SetValue(ShowActionProperty, value);
    }

    #endregion
}
