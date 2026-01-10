using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using VapourSynthPortable.Controls.Automation;
using VapourSynthPortable.Services.LibMpv;

namespace VapourSynthPortable.Controls;

public partial class VideoPlayerControl : UserControl
{
    protected override AutomationPeer OnCreateAutomationPeer()
        => new VideoPlayerControlAutomationPeer(this);

    private MpvPlayer? _player;
    private HwndHost? _videoHost;
    private IntPtr _hwnd;
    private bool _isSeeking;
    private bool _isMuted;
    private double _previousVolume = 100;
    private DispatcherTimer? _updateTimer;

    public event EventHandler<string>? FileLoaded;
    public event EventHandler? PlaybackEnded;

    public MpvPlayer? Player => _player;
    public bool IsPlaying => _player?.IsPlaying ?? false;
    public bool IsPaused => _player?.IsPaused ?? false;

    public VideoPlayerControl()
    {
        InitializeComponent();

        // Check if mpv is available
        if (!MpvPlayer.IsLibraryAvailable)
        {
            MpvStatusText.Text = "libmpv-2.dll not found. Please install mpv.";
        }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        SetupKeyboardShortcuts();

        // Start update timer for position sync
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();

        // Only initialize player when actually visible to avoid UI Automation issues
        // with HwndHost when control is collapsed
        if (IsVisible)
        {
            InitializePlayer();
        }
        else
        {
            IsVisibleChanged += OnVisibleChanged;
        }
    }

    private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue && _videoHost == null)
        {
            InitializePlayer();
        }
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _updateTimer?.Stop();
        _player?.Dispose();
        _player = null;
    }

    private void InitializePlayer()
    {
        if (!MpvPlayer.IsLibraryAvailable)
            return;

        try
        {
            // Create a native window to host mpv
            _videoHost = new MpvVideoHost();
            VideoHost.Child = _videoHost;

            // Wait for the HWND to be created
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                _hwnd = ((MpvVideoHost)_videoHost).Hwnd;
                if (_hwnd != IntPtr.Zero)
                {
                    _player = new MpvPlayer();
                    if (_player.Initialize(_hwnd))
                    {
                        PlaceholderPanel.Visibility = Visibility.Collapsed;
                        SetupPlayerEvents();
                        MpvStatusText.Text = "";
                    }
                    else
                    {
                        MpvStatusText.Text = "Failed to initialize mpv";
                    }
                }
            });
        }
        catch (Exception ex)
        {
            MpvStatusText.Text = $"Error: {ex.Message}";
        }
    }

    private void SetupPlayerEvents()
    {
        if (_player == null) return;

        _player.PositionChanged += (s, pos) =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (!_isSeeking)
                {
                    UpdateTimeDisplay(pos, _player.Duration);
                }
            });
        };

        _player.DurationChanged += (s, dur) =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                TimelineSlider.Maximum = dur;
                DurationText.Text = FormatTime(dur);
            });
        };

        _player.PlaybackStarted += (s, e) =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                PlayPauseButton.Content = "\uE769"; // Pause icon
                PlaceholderPanel.Visibility = Visibility.Collapsed;
                LoadingOverlay.Visibility = Visibility.Collapsed;
            });
        };

        _player.PlaybackEnded += (s, e) =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                PlayPauseButton.Content = "\uE768"; // Play icon
                PlaybackEnded?.Invoke(this, EventArgs.Empty);
            });
        };

        _player.PlaybackPaused += (s, e) =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                PlayPauseButton.Content = "\uE768"; // Play icon
            });
        };

        _player.PlaybackResumed += (s, e) =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                PlayPauseButton.Content = "\uE769"; // Pause icon
            });
        };

        _player.Error += (s, msg) =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                MpvStatusText.Text = msg;
                LoadingOverlay.Visibility = Visibility.Collapsed;
            });
        };
    }

    private void SetupKeyboardShortcuts()
    {
        var window = Window.GetWindow(this);
        if (window != null)
        {
            window.PreviewKeyDown += Window_PreviewKeyDown;
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsVisible || _player == null) return;

        switch (e.Key)
        {
            case Key.Space:
                _player.TogglePause();
                e.Handled = true;
                break;

            case Key.Left:
                _player.SeekRelative(-5);
                e.Handled = true;
                break;

            case Key.Right:
                _player.SeekRelative(5);
                e.Handled = true;
                break;

            case Key.Up:
                VolumeSlider.Value = Math.Min(100, VolumeSlider.Value + 5);
                e.Handled = true;
                break;

            case Key.Down:
                VolumeSlider.Value = Math.Max(0, VolumeSlider.Value - 5);
                e.Handled = true;
                break;

            case Key.M:
                ToggleMute();
                e.Handled = true;
                break;

            case Key.OemComma: // ,
                _player.StepBackward();
                e.Handled = true;
                break;

            case Key.OemPeriod: // .
                _player.StepForward();
                e.Handled = true;
                break;

            case Key.Home:
                _player.Seek(0);
                e.Handled = true;
                break;

            case Key.End:
                _player.Seek(_player.Duration - 1);
                e.Handled = true;
                break;
        }
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        if (_player == null || _isSeeking) return;

        var pos = _player.GetPosition();
        var dur = _player.GetDuration();
        UpdateTimeDisplay(pos, dur);
    }

    private void UpdateTimeDisplay(double position, double duration)
    {
        if (!_isSeeking && duration > 0)
        {
            TimelineSlider.Value = position;
        }
        CurrentTimeText.Text = FormatTime(position);
    }

    public void LoadFile(string filePath)
    {
        if (_player == null)
        {
            MpvStatusText.Text = "Player not initialized";
            PlaceholderPanel.Visibility = Visibility.Visible;
            return;
        }

        LoadingOverlay.Visibility = Visibility.Visible;
        _player.LoadFile(filePath);
        FileLoaded?.Invoke(this, filePath);
    }

    public void Play() => _player?.Play();
    public void Pause() => _player?.Pause();
    public void Stop() => _player?.Stop();
    public void Seek(double position) => _player?.Seek(position);

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        _player?.TogglePause();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _player?.Stop();
        TimelineSlider.Value = 0;
        CurrentTimeText.Text = FormatTime(0);
        PlaceholderPanel.Visibility = Visibility.Visible;
    }

    private void StepBackButton_Click(object sender, RoutedEventArgs e)
    {
        _player?.StepBackward();
    }

    private void StepForwardButton_Click(object sender, RoutedEventArgs e)
    {
        _player?.StepForward();
    }

    private void TimelineSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isSeeking = true;
    }

    private void TimelineSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isSeeking = false;
        _player?.Seek(TimelineSlider.Value);
    }

    private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isSeeking)
        {
            CurrentTimeText.Text = FormatTime(e.NewValue);
        }
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _player?.SetVolume(e.NewValue);

        // Null check for initialization
        if (VolumeText != null)
            VolumeText.Text = $"{e.NewValue:F0}%";

        // Update mute button icon
        if (MuteButton != null)
        {
            if (e.NewValue == 0)
                MuteButton.Content = "\uE74F"; // Muted
            else
                MuteButton.Content = "\uE767"; // Volume
        }
    }

    private void MuteButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleMute();
    }

    private void ToggleMute()
    {
        if (_isMuted)
        {
            VolumeSlider.Value = _previousVolume;
            _isMuted = false;
        }
        else
        {
            _previousVolume = VolumeSlider.Value;
            VolumeSlider.Value = 0;
            _isMuted = true;
        }
    }

    private void SpeedCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_player == null || SpeedCombo.SelectedItem == null) return;

        var content = (SpeedCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
        if (content != null && content.EndsWith("x"))
        {
            if (double.TryParse(content.TrimEnd('x'), out var speed))
            {
                _player.SetSpeed(speed);
            }
        }
    }

    private static string FormatTime(double seconds)
    {
        if (double.IsNaN(seconds) || seconds < 0) seconds = 0;
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.Hours > 0
            ? $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}

/// <summary>
/// HwndHost that creates a native window for mpv to render into.
/// Includes custom AutomationPeer to prevent UI Automation tree traversal issues.
/// </summary>
internal class MpvVideoHost : HwndHost
{
    public IntPtr Hwnd { get; private set; }

    private const int WS_CHILD = 0x40000000;
    private const int WS_VISIBLE = 0x10000000;
    private const int WS_CLIPCHILDREN = 0x02000000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        int dwExStyle, string lpClassName, string lpWindowName,
        int dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hwnd);

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        Hwnd = CreateWindowEx(
            0,
            "Static",
            "",
            WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN,
            0, 0,
            (int)ActualWidth,
            (int)ActualHeight,
            hwndParent.Handle,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);

        return new HandleRef(this, Hwnd);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        DestroyWindow(hwnd.Handle);
    }

    /// <summary>
    /// Returns a custom AutomationPeer that prevents UI Automation from
    /// traversing into the native window, which can cause timeouts.
    /// </summary>
    protected override AutomationPeer OnCreateAutomationPeer()
        => new MpvVideoHostAutomationPeer(this);
}

/// <summary>
/// AutomationPeer for MpvVideoHost that prevents UI Automation tree traversal
/// into the native window, which can cause timeouts and hangs.
/// </summary>
internal class MpvVideoHostAutomationPeer : FrameworkElementAutomationPeer
{
    public MpvVideoHostAutomationPeer(MpvVideoHost owner) : base(owner) { }

    protected override string GetClassNameCore() => "MpvVideoHost";

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

    protected override string GetLocalizedControlTypeCore() => "Video Host";

    protected override string GetNameCore() => "MPV Video Host";

    protected override bool IsContentElementCore() => false;

    protected override bool IsControlElementCore() => false;

    /// <summary>
    /// Returns an empty list to prevent UI Automation from traversing into
    /// the native window, which can cause timeouts.
    /// </summary>
    protected override List<AutomationPeer> GetChildrenCore() => new();
}
