using System.Collections.Concurrent;
using System.Windows.Threading;
using VapourSynthPortable.Controls;

namespace VapourSynthPortable.Services;

/// <summary>
/// Application-wide toast notification service with queue support.
/// </summary>
public class ToastService
{
    private static ToastService? _instance;
    public static ToastService Instance => _instance ??= new ToastService();

    private ToastNotification? _toast;
    private readonly ConcurrentQueue<ToastItem> _queue = new();
    private bool _isShowing;
    private DispatcherTimer? _queueTimer;

    // Default durations by type (in milliseconds)
    private const int InfoDuration = 3000;
    private const int SuccessDuration = 2500;
    private const int WarningDuration = 4000;
    private const int ErrorDuration = 5000;

    public void SetToastControl(ToastNotification toast)
    {
        _toast = toast;

        // Setup queue processing timer
        _queueTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _queueTimer.Tick += ProcessQueue;
    }

    public void Show(string message, ToastNotification.ToastType type = ToastNotification.ToastType.Info, string? detail = null, int? durationMs = null)
    {
        // Use type-specific duration if not specified
        var duration = durationMs ?? GetDefaultDuration(type);

        // Add to queue
        _queue.Enqueue(new ToastItem(message, type, detail, duration));

        // Start processing if not already
        if (!_isShowing && _queueTimer != null && !_queueTimer.IsEnabled)
        {
            ProcessNextToast();
        }
    }

    private void ProcessQueue(object? sender, EventArgs e)
    {
        if (!_isShowing && _queue.TryPeek(out _))
        {
            ProcessNextToast();
        }
        else if (_queue.IsEmpty)
        {
            _queueTimer?.Stop();
        }
    }

    private void ProcessNextToast()
    {
        if (_toast == null || !_queue.TryDequeue(out var item)) return;

        _isShowing = true;
        _toast.Show(item.Message, item.Type, item.Detail, item.DurationMs);

        // Schedule next toast after this one finishes
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(item.DurationMs + 300) // Add buffer for animation
        };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            _isShowing = false;

            // Process next in queue
            if (!_queue.IsEmpty)
            {
                ProcessNextToast();
            }
        };
        timer.Start();
    }

    private static int GetDefaultDuration(ToastNotification.ToastType type)
    {
        return type switch
        {
            ToastNotification.ToastType.Success => SuccessDuration,
            ToastNotification.ToastType.Warning => WarningDuration,
            ToastNotification.ToastType.Error => ErrorDuration,
            _ => InfoDuration
        };
    }

    public void ShowInfo(string message, string? detail = null) =>
        Show(message, ToastNotification.ToastType.Info, detail);

    public void ShowSuccess(string message, string? detail = null) =>
        Show(message, ToastNotification.ToastType.Success, detail);

    public void ShowWarning(string message, string? detail = null) =>
        Show(message, ToastNotification.ToastType.Warning, detail);

    public void ShowError(string message, string? detail = null) =>
        Show(message, ToastNotification.ToastType.Error, detail);

    /// <summary>
    /// Clears all pending notifications from the queue
    /// </summary>
    public void ClearQueue()
    {
        while (_queue.TryDequeue(out _)) { }
    }

    /// <summary>
    /// Gets the number of pending notifications
    /// </summary>
    public int PendingCount => _queue.Count;

    private record ToastItem(string Message, ToastNotification.ToastType Type, string? Detail, int DurationMs);
}
