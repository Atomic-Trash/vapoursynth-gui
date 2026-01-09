using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace VapourSynthPortable.Services.LibMpv;

/// <summary>
/// High-level wrapper for libmpv player functionality
/// </summary>
public class MpvPlayer : IDisposable
{
    private static readonly ILogger<MpvPlayer> _logger = LoggingService.GetLogger<MpvPlayer>();

    private IntPtr _handle;
    private bool _disposed;
    private Thread? _eventThread;
    private bool _eventThreadRunning;
    private static bool _libLoaded;
    private static string? _libPath;

    public event EventHandler? PlaybackStarted;
    public event EventHandler? PlaybackEnded;
    public event EventHandler? PlaybackPaused;
    public event EventHandler? PlaybackResumed;
    public event EventHandler<double>? PositionChanged;
    public event EventHandler<double>? DurationChanged;
    public event EventHandler<string>? Error;

    public bool IsInitialized => _handle != IntPtr.Zero;
    public bool IsPlaying { get; private set; }
    public bool IsPaused { get; private set; }
    public double Duration { get; private set; }
    public double Position { get; private set; }
    public double Volume { get; private set; } = 100;
    public double Speed { get; private set; } = 1.0;
    public double FrameRate { get; private set; } = 24.0;
    public string? CurrentFile { get; private set; }

    // AB Loop support
    public double? LoopStartPoint { get; private set; }
    public double? LoopEndPoint { get; private set; }
    public bool IsLoopEnabled => LoopStartPoint.HasValue && LoopEndPoint.HasValue;

    public static bool IsLibraryAvailable => FindLibrary() != null;

    private static string? FindProjectRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        for (int i = 0; i < 10 && dir != null; i++)
        {
            // Look for plugins.json as marker for project root
            if (File.Exists(Path.Combine(dir.FullName, "plugins.json")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static string? FindLibrary()
    {
        if (_libPath != null) return _libPath;

        var searchPaths = new List<string>();

        // Application directory
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        searchPaths.Add(Path.Combine(appDir, "libmpv-2.dll"));
        searchPaths.Add(Path.Combine(appDir, "mpv-2.dll"));

        // Project root dist folder (reliable method using plugins.json marker)
        var projectRoot = FindProjectRoot(appDir);
        if (projectRoot != null)
        {
            searchPaths.Add(Path.Combine(projectRoot, "dist", "mpv", "libmpv-2.dll"));
            searchPaths.Add(Path.Combine(projectRoot, "dist", "mpv", "mpv-2.dll"));
        }

        // Fallback: relative path from app directory (for deployed apps)
        var distPath = Path.GetFullPath(Path.Combine(appDir, "..", "..", "..", "..", "..", "dist"));
        searchPaths.Add(Path.Combine(distPath, "mpv", "libmpv-2.dll"));
        searchPaths.Add(Path.Combine(distPath, "mpv", "mpv-2.dll"));

        // Common install locations
        searchPaths.Add(@"C:\Program Files\mpv\libmpv-2.dll");
        searchPaths.Add(@"C:\Program Files\mpv\mpv-2.dll");

        // PATH environment
        var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? [];
        foreach (var dir in pathDirs)
        {
            searchPaths.Add(Path.Combine(dir, "libmpv-2.dll"));
            searchPaths.Add(Path.Combine(dir, "mpv-2.dll"));
        }

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                _libPath = Path.GetDirectoryName(path);
                _logger.LogInformation("Found libmpv at: {Path}", path);
                return _libPath;
            }
        }

        _logger.LogWarning("libmpv-2.dll not found in any search path. Searched: {Paths}",
            string.Join(", ", searchPaths.Take(5)));
        return null;
    }

    private static void LoadLibrary()
    {
        if (_libLoaded) return;

        var libDir = FindLibrary();
        if (libDir != null)
        {
            // Add to DLL search path
            SetDllDirectory(libDir);
            _libLoaded = true;
            _logger.LogInformation("libmpv library directory set: {Directory}", libDir);
        }
        else
        {
            _logger.LogError("Failed to find libmpv library. Video playback will not work");
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetDllDirectory(string lpPathName);

    public MpvPlayer()
    {
        LoadLibrary();
    }

    public bool Initialize(IntPtr windowHandle)
    {
        if (_handle != IntPtr.Zero)
            return true;

        _logger.LogInformation("Initializing MpvPlayer with window handle: {Handle}", windowHandle);

        try
        {
            _handle = LibMpvInterop.mpv_create();
            if (_handle == IntPtr.Zero)
            {
                _logger.LogError("Failed to create mpv instance - mpv_create returned null");
                Error?.Invoke(this, "Failed to create mpv instance");
                return false;
            }

            _logger.LogDebug("mpv instance created, setting options...");

            // Set window handle for video output
            var wid = windowHandle.ToInt64();
            LibMpvInterop.mpv_set_option(_handle, "wid", LibMpvInterop.MPV_FORMAT_INT64, ref wid);

            // Configuration options
            LibMpvInterop.mpv_set_option_string(_handle, "vo", "gpu");
            LibMpvInterop.mpv_set_option_string(_handle, "hwdec", "auto-safe");
            LibMpvInterop.mpv_set_option_string(_handle, "keep-open", "yes");
            LibMpvInterop.mpv_set_option_string(_handle, "idle", "yes");
            LibMpvInterop.mpv_set_option_string(_handle, "osc", "no");
            LibMpvInterop.mpv_set_option_string(_handle, "input-default-bindings", "no");
            LibMpvInterop.mpv_set_option_string(_handle, "input-vo-keyboard", "no");
            LibMpvInterop.mpv_set_option_string(_handle, "osd-level", "0");

            var result = LibMpvInterop.mpv_initialize(_handle);
            if (result < 0)
            {
                var errorMsg = LibMpvInterop.GetErrorString(result);
                _logger.LogError("Failed to initialize mpv: {Error} (code: {Code})", errorMsg, result);
                Error?.Invoke(this, $"Failed to initialize mpv: {errorMsg}");
                LibMpvInterop.mpv_destroy(_handle);
                _handle = IntPtr.Zero;
                return false;
            }

            // Observe properties
            LibMpvInterop.mpv_observe_property(_handle, 1, "time-pos", LibMpvInterop.MPV_FORMAT_DOUBLE);
            LibMpvInterop.mpv_observe_property(_handle, 2, "duration", LibMpvInterop.MPV_FORMAT_DOUBLE);
            LibMpvInterop.mpv_observe_property(_handle, 3, "pause", LibMpvInterop.MPV_FORMAT_FLAG);
            LibMpvInterop.mpv_observe_property(_handle, 4, "eof-reached", LibMpvInterop.MPV_FORMAT_FLAG);

            // Start event loop
            StartEventLoop();

            _logger.LogInformation("MpvPlayer initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception initializing mpv");
            Error?.Invoke(this, $"Exception initializing mpv: {ex.Message}");
            return false;
        }
    }

    private void StartEventLoop()
    {
        _eventThreadRunning = true;
        _eventThread = new Thread(EventLoop)
        {
            IsBackground = true,
            Name = "MpvEventLoop"
        };
        _eventThread.Start();
    }

    private void EventLoop()
    {
        while (_eventThreadRunning && _handle != IntPtr.Zero)
        {
            try
            {
                var eventPtr = LibMpvInterop.mpv_wait_event(_handle, 0.1);
                if (eventPtr == IntPtr.Zero) continue;

                var evt = Marshal.PtrToStructure<LibMpvInterop.MpvEvent>(eventPtr);

                switch (evt.event_id)
                {
                    case LibMpvInterop.MPV_EVENT_PROPERTY_CHANGE:
                        HandlePropertyChange(evt);
                        break;

                    case LibMpvInterop.MPV_EVENT_FILE_LOADED:
                        IsPlaying = true;
                        PlaybackStarted?.Invoke(this, EventArgs.Empty);
                        break;

                    case LibMpvInterop.MPV_EVENT_END_FILE:
                        IsPlaying = false;
                        PlaybackEnded?.Invoke(this, EventArgs.Empty);
                        break;

                    case LibMpvInterop.MPV_EVENT_SHUTDOWN:
                        _eventThreadRunning = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "mpv event processing error");
            }
        }
    }

    private void HandlePropertyChange(LibMpvInterop.MpvEvent evt)
    {
        if (evt.data == IntPtr.Zero) return;

        var prop = Marshal.PtrToStructure<LibMpvInterop.MpvEventProperty>(evt.data);
        var name = Marshal.PtrToStringAnsi(prop.name);

        switch (name)
        {
            case "time-pos":
                if (prop.format == LibMpvInterop.MPV_FORMAT_DOUBLE && prop.data != IntPtr.Zero)
                {
                    Position = Marshal.PtrToStructure<double>(prop.data);
                    PositionChanged?.Invoke(this, Position);
                }
                break;

            case "duration":
                if (prop.format == LibMpvInterop.MPV_FORMAT_DOUBLE && prop.data != IntPtr.Zero)
                {
                    Duration = Marshal.PtrToStructure<double>(prop.data);
                    DurationChanged?.Invoke(this, Duration);
                }
                break;

            case "pause":
                if (prop.format == LibMpvInterop.MPV_FORMAT_FLAG && prop.data != IntPtr.Zero)
                {
                    var paused = Marshal.PtrToStructure<int>(prop.data) != 0;
                    IsPaused = paused;
                    if (paused)
                        PlaybackPaused?.Invoke(this, EventArgs.Empty);
                    else
                        PlaybackResumed?.Invoke(this, EventArgs.Empty);
                }
                break;
        }
    }

    public void LoadFile(string path)
    {
        if (_handle == IntPtr.Zero)
        {
            _logger.LogWarning("LoadFile called but mpv not initialized");
            return;
        }

        _logger.LogInformation("Loading file: {Path}", path);
        CurrentFile = path;
        Command($"loadfile \"{path.Replace("\\", "/")}\"");
    }

    public void Play()
    {
        if (_handle == IntPtr.Zero) return;

        SetProperty("pause", "no");
        IsPaused = false;
    }

    public void Pause()
    {
        if (_handle == IntPtr.Zero) return;

        SetProperty("pause", "yes");
        IsPaused = true;
    }

    public void TogglePause()
    {
        if (IsPaused)
            Play();
        else
            Pause();
    }

    public void Stop()
    {
        if (_handle == IntPtr.Zero) return;

        Command("stop");
        IsPlaying = false;
        IsPaused = false;
        Position = 0;
        CurrentFile = null;
    }

    public void Seek(double position)
    {
        if (_handle == IntPtr.Zero) return;

        Command($"seek {position:F3} absolute");
    }

    public void SeekRelative(double seconds)
    {
        if (_handle == IntPtr.Zero) return;

        Command($"seek {seconds:F3} relative");
    }

    public void SetVolume(double volume)
    {
        if (_handle == IntPtr.Zero) return;

        Volume = Math.Clamp(volume, 0, 100);
        SetProperty("volume", Volume.ToString("F0"));
    }

    public void SetSpeed(double speed)
    {
        if (_handle == IntPtr.Zero) return;

        speed = Math.Clamp(speed, 0.25, 4.0);
        SetProperty("speed", speed.ToString("F2"));
    }

    public void StepForward()
    {
        if (_handle == IntPtr.Zero) return;

        Command("frame-step");
    }

    public void StepBackward()
    {
        if (_handle == IntPtr.Zero) return;

        Command("frame-back-step");
    }

    public void SetLoop(bool loop)
    {
        if (_handle == IntPtr.Zero) return;

        SetProperty("loop-file", loop ? "inf" : "no");
    }

    /// <summary>
    /// Seek to a specific frame number
    /// </summary>
    public void SeekToFrame(long frame, double? frameRate = null)
    {
        var fps = frameRate ?? FrameRate;
        if (fps <= 0) fps = 24.0;

        var position = frame / fps;
        Seek(position);
    }

    /// <summary>
    /// Get the current frame number
    /// </summary>
    public long GetCurrentFrame(double? frameRate = null)
    {
        var fps = frameRate ?? FrameRate;
        if (fps <= 0) fps = 24.0;

        return (long)(Position * fps);
    }

    /// <summary>
    /// Set the frame rate for frame-based operations
    /// </summary>
    public void SetFrameRate(double fps)
    {
        FrameRate = fps > 0 ? fps : 24.0;
    }

    /// <summary>
    /// Set AB loop points for region playback
    /// </summary>
    public void SetABLoop(double? startPoint, double? endPoint)
    {
        if (_handle == IntPtr.Zero) return;

        LoopStartPoint = startPoint;
        LoopEndPoint = endPoint;

        if (startPoint.HasValue && endPoint.HasValue)
        {
            SetProperty("ab-loop-a", startPoint.Value.ToString("F3"));
            SetProperty("ab-loop-b", endPoint.Value.ToString("F3"));
            _logger.LogDebug("AB Loop set: {Start} - {End}", startPoint, endPoint);
        }
        else
        {
            ClearABLoop();
        }
    }

    /// <summary>
    /// Set AB loop using frame numbers
    /// </summary>
    public void SetABLoopFrames(long startFrame, long endFrame, double? frameRate = null)
    {
        var fps = frameRate ?? FrameRate;
        if (fps <= 0) fps = 24.0;

        SetABLoop(startFrame / fps, endFrame / fps);
    }

    /// <summary>
    /// Clear AB loop points
    /// </summary>
    public void ClearABLoop()
    {
        if (_handle == IntPtr.Zero) return;

        LoopStartPoint = null;
        LoopEndPoint = null;
        SetProperty("ab-loop-a", "no");
        SetProperty("ab-loop-b", "no");
        _logger.LogDebug("AB Loop cleared");
    }

    /// <summary>
    /// Toggle AB loop - sets A point first, then B point, then clears
    /// </summary>
    public void ToggleABLoopPoint()
    {
        if (!LoopStartPoint.HasValue)
        {
            LoopStartPoint = Position;
            SetProperty("ab-loop-a", Position.ToString("F3"));
            _logger.LogDebug("AB Loop A point set: {Position}", Position);
        }
        else if (!LoopEndPoint.HasValue)
        {
            LoopEndPoint = Position;
            SetProperty("ab-loop-b", Position.ToString("F3"));
            _logger.LogDebug("AB Loop B point set: {Position}", Position);
        }
        else
        {
            ClearABLoop();
        }
    }

    /// <summary>
    /// Playback speed control - J/K/L style
    /// J = slower/reverse, K = pause, L = faster
    /// </summary>
    private static readonly double[] _speedLevels = [0.25, 0.5, 1.0, 1.5, 2.0, 3.0, 4.0];
    private int _currentSpeedIndex = 2; // Start at 1.0x

    public void SpeedUp()
    {
        if (_handle == IntPtr.Zero) return;

        if (_currentSpeedIndex < _speedLevels.Length - 1)
        {
            _currentSpeedIndex++;
            Speed = _speedLevels[_currentSpeedIndex];
            SetSpeed(Speed);
            _logger.LogDebug("Speed increased to {Speed}x", Speed);
        }
    }

    public void SlowDown()
    {
        if (_handle == IntPtr.Zero) return;

        if (_currentSpeedIndex > 0)
        {
            _currentSpeedIndex--;
            Speed = _speedLevels[_currentSpeedIndex];
            SetSpeed(Speed);
            _logger.LogDebug("Speed decreased to {Speed}x", Speed);
        }
    }

    public void ResetSpeed()
    {
        if (_handle == IntPtr.Zero) return;

        _currentSpeedIndex = 2;
        Speed = 1.0;
        SetSpeed(1.0);
        _logger.LogDebug("Speed reset to 1.0x");
    }

    /// <summary>
    /// Step forward by specified number of frames
    /// </summary>
    public void StepForwardFrames(int frames)
    {
        if (_handle == IntPtr.Zero || frames <= 0) return;

        for (int i = 0; i < frames; i++)
        {
            Command("frame-step");
        }
    }

    /// <summary>
    /// Step backward by specified number of frames
    /// </summary>
    public void StepBackwardFrames(int frames)
    {
        if (_handle == IntPtr.Zero || frames <= 0) return;

        for (int i = 0; i < frames; i++)
        {
            Command("frame-back-step");
        }
    }

    /// <summary>
    /// Seek to the beginning of the file
    /// </summary>
    public void SeekToStart()
    {
        Seek(0);
    }

    /// <summary>
    /// Seek to the end of the file
    /// </summary>
    public void SeekToEnd()
    {
        if (Duration > 0)
        {
            Seek(Duration - 0.001);
        }
    }

    /// <summary>
    /// Seek forward by a percentage of total duration
    /// </summary>
    public void SeekPercent(double percent)
    {
        if (_handle == IntPtr.Zero || Duration <= 0) return;

        var position = Duration * (percent / 100.0);
        Seek(Math.Clamp(position, 0, Duration));
    }

    private void Command(string cmd)
    {
        if (_handle == IntPtr.Zero) return;

        var result = LibMpvInterop.mpv_command_string(_handle, cmd);
        if (result < 0)
        {
            _logger.LogWarning("mpv command failed: {Command} - {Error}", cmd, LibMpvInterop.GetErrorString(result));
        }
    }

    private void SetProperty(string name, string value)
    {
        if (_handle == IntPtr.Zero) return;

        LibMpvInterop.mpv_set_property_string(_handle, name, value);
    }

    public double GetDuration()
    {
        if (_handle == IntPtr.Zero) return 0;

        if (LibMpvInterop.mpv_get_property(_handle, "duration", LibMpvInterop.MPV_FORMAT_DOUBLE, out double duration) >= 0)
        {
            Duration = duration;
            return duration;
        }
        return 0;
    }

    public double GetPosition()
    {
        if (_handle == IntPtr.Zero) return 0;

        if (LibMpvInterop.mpv_get_property(_handle, "time-pos", LibMpvInterop.MPV_FORMAT_DOUBLE, out double pos) >= 0)
        {
            Position = pos;
            return pos;
        }
        return 0;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        _logger.LogInformation("Disposing MpvPlayer");
        _eventThreadRunning = false;

        if (_handle != IntPtr.Zero)
        {
            try
            {
                LibMpvInterop.mpv_terminate_destroy(_handle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminating mpv");
            }
            _handle = IntPtr.Zero;
        }

        _eventThread?.Join(1000);

        _disposed = true;
        _logger.LogInformation("MpvPlayer disposed");
    }

    ~MpvPlayer()
    {
        Dispose(false);
    }
}
