using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace VapourSynthPortable.Controls;

public partial class ToastNotification : UserControl
{
    private readonly DispatcherTimer _hideTimer;
    private bool _isVisible;

    public ToastNotification()
    {
        InitializeComponent();

        _hideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _hideTimer.Tick += (s, e) => Hide();
    }

    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public void Show(string message, ToastType type = ToastType.Info, string? detail = null, int durationMs = 3000)
    {
        // Stop any existing timer
        _hideTimer.Stop();

        // Set message
        MessageText.Text = message;

        // Set detail
        if (!string.IsNullOrEmpty(detail))
        {
            DetailText.Text = detail;
            DetailText.Visibility = Visibility.Visible;
        }
        else
        {
            DetailText.Visibility = Visibility.Collapsed;
        }

        // Set icon and colors based on type
        IconBorder.Visibility = Visibility.Visible;
        switch (type)
        {
            case ToastType.Success:
                IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x5A, 0x2D));
                IconText.Text = "\uE73E"; // Checkmark
                IconText.Foreground = new SolidColorBrush(Color.FromRgb(0x5C, 0xB8, 0x5C));
                break;

            case ToastType.Warning:
                IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x5A, 0x4A, 0x2D));
                IconText.Text = "\uE7BA"; // Warning
                IconText.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));
                break;

            case ToastType.Error:
                IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x5A, 0x2D, 0x2D));
                IconText.Text = "\uE711"; // Error X
                IconText.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0x66));
                break;

            case ToastType.Info:
            default:
                IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x3A, 0x5A));
                IconText.Text = "\uE946"; // Info
                IconText.Foreground = new SolidColorBrush(Color.FromRgb(0x7C, 0x7F, 0xFF));
                break;
        }

        // Run show animation
        var showAnim = (Storyboard)Resources["ShowAnimation"];
        showAnim.Begin(this);
        _isVisible = true;

        // Start hide timer
        _hideTimer.Interval = TimeSpan.FromMilliseconds(durationMs);
        _hideTimer.Start();
    }

    public void Hide()
    {
        if (!_isVisible) return;

        _hideTimer.Stop();

        var hideAnim = (Storyboard)Resources["HideAnimation"];
        hideAnim.Completed += (s, e) => _isVisible = false;
        hideAnim.Begin(this);
    }

    // Convenience methods
    public void ShowInfo(string message, string? detail = null) => Show(message, ToastType.Info, detail);
    public void ShowSuccess(string message, string? detail = null) => Show(message, ToastType.Success, detail);
    public void ShowWarning(string message, string? detail = null) => Show(message, ToastType.Warning, detail);
    public void ShowError(string message, string? detail = null) => Show(message, ToastType.Error, detail);
}
