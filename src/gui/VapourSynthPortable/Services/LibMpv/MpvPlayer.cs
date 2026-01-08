using System.IO;
using System.Runtime.InteropServices;

namespace VapourSynthPortable.Services.LibMpv;

/// <summary>
/// High-level wrapper for libmpv player functionality
/// </summary>
public class MpvPlayer : IDisposable
{
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
    public string? CurrentFile { get; private set; }

    public static bool IsLibraryAvailable => FindLibrary() != null;

    private static string? FindLibrary()
    {
        if (_libPath != null) return _libPath;

        var searchPaths = new List<string>();

        // Application directory
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        searchPaths.Add(Path.Combine(appDir, "libmpv-2.dll"));
        searchPaths.Add(Path.Combine(appDir, "mpv-2.dll"));

        // Dist folder structure
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
                return _libPath;
            }
        }

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

        try
        {
            _handle = LibMpvInterop.mpv_create();
            if (_handle == IntPtr.Zero)
            {
                Error?.Invoke(this, "Failed to create mpv instance");
                return false;
            }

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
                Error?.Invoke(this, $"Failed to initialize mpv: {LibMpvInterop.GetErrorString(result)}");
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

            return true;
        }
        catch (Exception ex)
        {
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
            catch
            {
                // Ignore event processing errors
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
        if (_handle == IntPtr.Zero) return;

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

    private void Command(string cmd)
    {
        if (_handle == IntPtr.Zero) return;

        var result = LibMpvInterop.mpv_command_string(_handle, cmd);
        if (result < 0)
        {
            System.Diagnostics.Debug.WriteLine($"mpv command failed: {cmd} - {LibMpvInterop.GetErrorString(result)}");
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

        _eventThreadRunning = false;

        if (_handle != IntPtr.Zero)
        {
            try
            {
                LibMpvInterop.mpv_terminate_destroy(_handle);
            }
            catch { }
            _handle = IntPtr.Zero;
        }

        _eventThread?.Join(1000);

        _disposed = true;
    }

    ~MpvPlayer()
    {
        Dispose(false);
    }
}
