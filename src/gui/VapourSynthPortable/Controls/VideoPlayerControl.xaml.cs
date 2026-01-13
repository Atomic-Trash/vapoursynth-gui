using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Controls.Automation;
using VapourSynthPortable.Services;
using VapourSynthPortable.Services.LibMpv;

namespace VapourSynthPortable.Controls;

public partial class VideoPlayerControl : UserControl
{
    private static readonly ILogger<VideoPlayerControl> _logger = LoggingService.GetLogger<VideoPlayerControl>();

    protected override AutomationPeer OnCreateAutomationPeer()
        => new VideoPlayerControlAutomationPeer(this);

    // Use DependencyPropertyDescriptor for reliable visibility change detection
    // (IsVisibleChanged event does NOT fire when visibility changes through WPF binding)
    private static readonly DependencyPropertyDescriptor? VisibilityDescriptor =
        DependencyPropertyDescriptor.FromProperty(VisibilityProperty, typeof(VideoPlayerControl));

    private MpvPlayer? _player;
    private HwndHost? _videoHost;
    private IntPtr _hwnd;
    private bool _isSeeking;
    private bool _isMuted;
    private double _previousVolume = 100;
    private DispatcherTimer? _updateTimer;

    // Race condition fix: track initialization state and pending operations
    private bool _playerInitialized;
    private string? _pendingFilePath;
    private TaskCompletionSource? _initializationTcs;
    private bool _visibilityDescriptorSubscribed;

    public event EventHandler<string>? FileLoaded;
    public event EventHandler? PlaybackEnded;
    public event EventHandler<double>? PositionChanged;

    public MpvPlayer? Player => _player;
    public bool IsPlaying => _player?.IsPlaying ?? false;
    public bool IsPaused => _player?.IsPaused ?? false;

    /// <summary>
    /// Returns true when the player is fully initialized and ready to accept commands.
    /// </summary>
    public bool IsPlayerReady => _playerInitialized && _player != null;

    /// <summary>
    /// Waits for the player to complete initialization. Returns immediately if already initialized.
    /// </summary>
    public Task WaitForInitializationAsync()
    {
        if (IsPlayerReady) return Task.CompletedTask;
        if (!MpvPlayer.IsLibraryAvailable) return Task.CompletedTask; // Will never initialize

        _initializationTcs ??= new TaskCompletionSource();
        return _initializationTcs.Task;
    }

    public VideoPlayerControl()
    {
        InitializeComponent();

        _logger.LogDebug("VideoPlayerControl constructed, Name={Name}", Name);

        // Subscribe to visibility changes early (before Loaded)
        SubscribeToVisibilityChanges();

        // Check if mpv is available
        if (!MpvPlayer.IsLibraryAvailable)
        {
            MpvStatusText.Text = "libmpv-2.dll not found. Video playback unavailable.";
            // Notify user prominently on first load
            Loaded += (s, e) =>
            {
                ToastService.Instance.ShowWarning(
                    "Video playback unavailable",
                    "libmpv not found. Run scripts/util/install-mpv.ps1 to enable video.");
            };
        }
    }

    private void SubscribeToVisibilityChanges()
    {
        if (_visibilityDescriptorSubscribed) return;

        if (VisibilityDescriptor != null)
        {
            VisibilityDescriptor.AddValueChanged(this, OnVisibilityPropertyChanged);
            _visibilityDescriptorSubscribed = true;
            _logger.LogDebug("Subscribed to VisibilityDescriptor");
        }
        else
        {
            _logger.LogWarning("VisibilityDescriptor is null, falling back to IsVisibleChanged event");
            IsVisibleChanged += OnIsVisibleChangedFallback;
        }
    }

    private void OnIsVisibleChangedFallback(object sender, DependencyPropertyChangedEventArgs e)
    {
        _logger.LogDebug("IsVisibleChanged fallback fired: NewValue={NewValue}", e.NewValue);
        if ((bool)e.NewValue && _videoHost == null)
        {
            InitializePlayer();
        }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        _logger.LogDebug("UserControl_Loaded fired, Name={Name}, IsVisible={IsVisible}, Visibility={Visibility}",
            Name, IsVisible, Visibility);

        SetupKeyboardShortcuts();

        // Start update timer for position sync
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();

        // Ensure we're subscribed (should already be from constructor, but be safe)
        SubscribeToVisibilityChanges();

        // Only initialize player when actually visible to avoid UI Automation issues
        // with HwndHost when control is collapsed
        if (IsVisible)
        {
            _logger.LogDebug("Control is visible on load, initializing player");
            InitializePlayer();
        }
        else
        {
            _logger.LogDebug("Control is NOT visible on load, waiting for visibility change");
        }
    }

    /// <summary>
    /// Called when visibility changes via DependencyPropertyDescriptor.
    /// This fires for ALL visibility changes including binding-driven ones.
    /// </summary>
    private void OnVisibilityPropertyChanged(object? sender, EventArgs e)
    {
        _logger.LogDebug("OnVisibilityPropertyChanged fired, Name={Name}, IsVisible={IsVisible}, Visibility={Visibility}, _videoHost={HasHost}",
            Name, IsVisible, Visibility, _videoHost != null);

        if (IsVisible && _videoHost == null)
        {
            _logger.LogInformation("Visibility changed to visible, initializing player for {Name}", Name);
            InitializePlayer();
        }
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _logger.LogDebug("UserControl_Unloaded fired, Name={Name}", Name);
        _updateTimer?.Stop();

        if (_visibilityDescriptorSubscribed && VisibilityDescriptor != null)
        {
            VisibilityDescriptor.RemoveValueChanged(this, OnVisibilityPropertyChanged);
            _visibilityDescriptorSubscribed = false;
        }

        IsVisibleChanged -= OnIsVisibleChangedFallback;

        _player?.Dispose();
        _player = null;
    }

    private void InitializePlayer()
    {
        _logger.LogDebug("InitializePlayer called, Name={Name}, LibraryAvailable={Available}, AlreadyHasHost={HasHost}",
            Name, MpvPlayer.IsLibraryAvailable, _videoHost != null);

        if (!MpvPlayer.IsLibraryAvailable)
        {
            _logger.LogWarning("Cannot initialize player - libmpv not available");
            return;
        }

        if (_videoHost != null)
        {
            _logger.LogDebug("Player already has video host, skipping initialization");
            return;
        }

        try
        {
            _logger.LogInformation("Creating MpvVideoHost for {Name}", Name);

            // Create a native window to host mpv
            _videoHost = new MpvVideoHost();
            VideoHost.Child = _videoHost;

            // Wait for the HWND to be created
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                _hwnd = ((MpvVideoHost)_videoHost).Hwnd;
                _logger.LogDebug("HWND created: {Hwnd}", _hwnd);

                if (_hwnd != IntPtr.Zero)
                {
                    _player = new MpvPlayer();
                    if (_player.Initialize(_hwnd))
                    {
                        _logger.LogInformation("MpvPlayer initialized successfully for {Name}", Name);
                        PlaceholderPanel.Visibility = Visibility.Collapsed;
                        SetupPlayerEvents();
                        MpvStatusText.Text = "";

                        // Signal initialization complete and process pending files
                        CompleteInitialization();
                    }
                    else
                    {
                        _logger.LogError("Failed to initialize MpvPlayer for {Name}", Name);
                        MpvStatusText.Text = "Failed to initialize mpv";
                    }
                }
                else
                {
                    _logger.LogError("HWND is zero for {Name}", Name);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in InitializePlayer for {Name}", Name);
            MpvStatusText.Text = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Called when player initialization is complete. Signals waiting tasks and processes pending files.
    /// </summary>
    private void CompleteInitialization()
    {
        _logger.LogInformation("CompleteInitialization for {Name}, pending file: {PendingPath}",
            Name, _pendingFilePath ?? "(none)");

        _playerInitialized = true;
        _initializationTcs?.TrySetResult();

        // Process any pending file load
        if (!string.IsNullOrEmpty(_pendingFilePath))
        {
            var path = _pendingFilePath;
            _pendingFilePath = null;
            _logger.LogDebug("Processing pending file load: {Path}", path);
            LoadFile(path);
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
                PositionChanged?.Invoke(this, pos);
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
                PlayPauseIcon.Text = "\uE769"; // Pause icon
                PlaceholderPanel.Visibility = Visibility.Collapsed;
                LoadingOverlay.Visibility = Visibility.Collapsed;
            });
        };

        _player.PlaybackEnded += (s, e) =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                PlayPauseIcon.Text = "\uE768"; // Play icon
                PlaybackEnded?.Invoke(this, EventArgs.Empty);
            });
        };

        _player.PlaybackPaused += (s, e) =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                PlayPauseIcon.Text = "\uE768"; // Play icon
            });
        };

        _player.PlaybackResumed += (s, e) =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                PlayPauseIcon.Text = "\uE769"; // Pause icon
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
        _logger.LogDebug("LoadFile called, Name={Name}, Path={Path}, IsVisible={IsVisible}, IsPlayerReady={Ready}",
            Name, filePath, IsVisible, IsPlayerReady);

        if (string.IsNullOrEmpty(filePath)) return;

        // If we're visible but player not initialized, initialize now
        // This handles cases where visibility changed but OnVisibilityPropertyChanged didn't fire yet
        if (IsVisible && _videoHost == null && MpvPlayer.IsLibraryAvailable)
        {
            _logger.LogDebug("LoadFile triggering deferred initialization for {Name}", Name);
            InitializePlayer();
        }

        // If player isn't ready yet, queue the file for later loading
        if (!IsPlayerReady || _player == null)
        {
            _logger.LogDebug("Player not ready, queuing file for later: {Path}", filePath);
            _pendingFilePath = filePath;

            // Show loading state while waiting for initialization
            if (MpvPlayer.IsLibraryAvailable)
            {
                MpvStatusText.Text = "Initializing player...";
                LoadingOverlay.Visibility = Visibility.Visible;
                PlaceholderPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                MpvStatusText.Text = "libmpv-2.dll not found. Video playback unavailable.";
                PlaceholderPanel.Visibility = Visibility.Visible;
            }
            return;
        }

        _logger.LogInformation("Loading file into player: {Path}", filePath);
        _pendingFilePath = null;
        LoadingOverlay.Visibility = Visibility.Visible;
        PlaceholderPanel.Visibility = Visibility.Collapsed;
        _player.LoadFile(filePath);
        FileLoaded?.Invoke(this, filePath);
    }

    public void Play() => _player?.Play();
    public void Pause() => _player?.Pause();
    public void Stop() => _player?.Stop();
    public void Seek(double position) => _player?.Seek(position);

    /// <summary>
    /// Capture the current video frame as RGB data for scopes analysis.
    /// </summary>
    public (byte[] RgbData, int Width, int Height)? CaptureCurrentFrame()
    {
        return _player?.CaptureCurrentFrame();
    }

    /// <summary>
    /// Capture the current video frame asynchronously.
    /// </summary>
    public Task<(byte[] RgbData, int Width, int Height)?> CaptureCurrentFrameAsync()
    {
        return _player?.CaptureCurrentFrameAsync() ?? Task.FromResult<(byte[], int, int)?>(null);
    }

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
                MuteIcon.Text = "\uE74F"; // Muted
            else
                MuteIcon.Text = "\uE767"; // Volume
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
