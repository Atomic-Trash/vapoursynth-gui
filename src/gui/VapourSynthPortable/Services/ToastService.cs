using VapourSynthPortable.Controls;

namespace VapourSynthPortable.Services;

/// <summary>
/// Application-wide toast notification service.
/// </summary>
public class ToastService
{
    private static ToastService? _instance;
    public static ToastService Instance => _instance ??= new ToastService();

    private ToastNotification? _toast;

    public void SetToastControl(ToastNotification toast)
    {
        _toast = toast;
    }

    public void Show(string message, ToastNotification.ToastType type = ToastNotification.ToastType.Info, string? detail = null, int durationMs = 3000)
    {
        _toast?.Show(message, type, detail, durationMs);
    }

    public void ShowInfo(string message, string? detail = null) => Show(message, ToastNotification.ToastType.Info, detail);
    public void ShowSuccess(string message, string? detail = null) => Show(message, ToastNotification.ToastType.Success, detail);
    public void ShowWarning(string message, string? detail = null) => Show(message, ToastNotification.ToastType.Warning, detail);
    public void ShowError(string message, string? detail = null) => Show(message, ToastNotification.ToastType.Error, detail);
}
