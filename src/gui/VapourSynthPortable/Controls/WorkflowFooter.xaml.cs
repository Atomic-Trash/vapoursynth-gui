using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VapourSynthPortable.Controls;

/// <summary>
/// Workflow footer control that displays status and a "Next Step" button for guided navigation.
/// </summary>
public partial class WorkflowFooter : UserControl
{
    public WorkflowFooter()
    {
        InitializeComponent();
    }

    #region Dependency Properties

    public static readonly DependencyProperty StatusTextProperty =
        DependencyProperty.Register(
            nameof(StatusText),
            typeof(string),
            typeof(WorkflowFooter),
            new PropertyMetadata("Ready"));

    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public static readonly DependencyProperty ShowStatusIconProperty =
        DependencyProperty.Register(
            nameof(ShowStatusIcon),
            typeof(bool),
            typeof(WorkflowFooter),
            new PropertyMetadata(false));

    public bool ShowStatusIcon
    {
        get => (bool)GetValue(ShowStatusIconProperty);
        set => SetValue(ShowStatusIconProperty, value);
    }

    public static readonly DependencyProperty NextStepTextProperty =
        DependencyProperty.Register(
            nameof(NextStepText),
            typeof(string),
            typeof(WorkflowFooter),
            new PropertyMetadata("Continue"));

    public string NextStepText
    {
        get => (string)GetValue(NextStepTextProperty);
        set => SetValue(NextStepTextProperty, value);
    }

    public static readonly DependencyProperty ShowNextStepProperty =
        DependencyProperty.Register(
            nameof(ShowNextStep),
            typeof(bool),
            typeof(WorkflowFooter),
            new PropertyMetadata(true));

    public bool ShowNextStep
    {
        get => (bool)GetValue(ShowNextStepProperty);
        set => SetValue(ShowNextStepProperty, value);
    }

    public static readonly DependencyProperty NextStepCommandProperty =
        DependencyProperty.Register(
            nameof(NextStepCommand),
            typeof(ICommand),
            typeof(WorkflowFooter),
            new PropertyMetadata(null));

    public ICommand? NextStepCommand
    {
        get => (ICommand?)GetValue(NextStepCommandProperty);
        set => SetValue(NextStepCommandProperty, value);
    }

    #endregion
}
