using System.IO;
using Microsoft.Extensions.Logging;

namespace VapourSynthPortable.Services;

/// <summary>
/// Centralized service for resolving paths to external dependencies.
/// Handles both development (running from source) and deployed scenarios.
/// </summary>
public class PathResolver : IPathResolver
{
    private static readonly ILogger<PathResolver> _logger = LoggingService.GetLogger<PathResolver>();

    private readonly string _appDirectory;
    private readonly string? _projectRoot;
    private readonly string _distPath;

    // Cached paths
    private string? _ffmpegPath;
    private string? _ffprobePath;
    private string? _vspipePath;
    private string? _pythonPath;

    public PathResolver()
    {
        _appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _projectRoot = FindProjectRoot(_appDirectory);
        _distPath = DetermineDistPath();

        _logger.LogInformation(
            "PathResolver initialized. AppDir: {AppDir}, ProjectRoot: {ProjectRoot}, DistPath: {DistPath}",
            _appDirectory, _projectRoot ?? "(not found)", _distPath);
    }

    /// <summary>
    /// The application's base directory
    /// </summary>
    public string AppDirectory => _appDirectory;

    /// <summary>
    /// The project root directory (contains plugins.json), or null if not found
    /// </summary>
    public string? ProjectRoot => _projectRoot;

    /// <summary>
    /// The distribution directory containing dependencies
    /// </summary>
    public string DistPath => _distPath;

    /// <summary>
    /// Path to FFmpeg executable, or null if not found
    /// </summary>
    public string? FFmpegPath => _ffmpegPath ??= FindFFmpeg();

    /// <summary>
    /// Path to FFprobe executable, or null if not found
    /// </summary>
    public string? FFprobePath => _ffprobePath ??= FindFFprobe();

    /// <summary>
    /// Path to VSPipe executable, or null if not found
    /// </summary>
    public string? VSPipePath => _vspipePath ??= FindVSPipe();

    /// <summary>
    /// Path to Python directory, or null if not found
    /// </summary>
    public string? PythonPath => _pythonPath ??= FindPythonPath();

    /// <summary>
    /// Path to VapourSynth directory
    /// </summary>
    public string VapourSynthPath => Path.Combine(_distPath, "vapoursynth");

    /// <summary>
    /// Path to plugins directory
    /// </summary>
    public string PluginsPath => Path.Combine(_distPath, "plugins");

    /// <summary>
    /// Whether FFmpeg is available
    /// </summary>
    public bool IsFFmpegAvailable => FFmpegPath != null && File.Exists(FFmpegPath);

    /// <summary>
    /// Whether VapourSynth is available
    /// </summary>
    public bool IsVapourSynthAvailable => VSPipePath != null && File.Exists(VSPipePath);

    /// <summary>
    /// Whether Python is available
    /// </summary>
    public bool IsPythonAvailable => PythonPath != null && Directory.Exists(PythonPath);

    /// <summary>
    /// Find project root by looking for plugins.json marker file
    /// </summary>
    private static string? FindProjectRoot(string startDir)
    {
        // Strategy 1: Walk up from app directory looking for plugins.json
        var dir = new DirectoryInfo(startDir);
        for (int i = 0; i < 10 && dir != null; i++)
        {
            if (File.Exists(Path.Combine(dir.FullName, "plugins.json")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        // Strategy 2: Check common development layouts
        // When running from bin/Debug/net8.0-windows, go up to find project root
        var possibleRoots = new[]
        {
            Path.GetFullPath(Path.Combine(startDir, "..", "..", "..", "..", "..")), // From bin/Debug/net8.0-windows/
            Path.GetFullPath(Path.Combine(startDir, "..", "..", "..", "..")),       // Shorter path
            Path.GetFullPath(Path.Combine(startDir, "..", "..", "..")),             // Even shorter
        };

        foreach (var root in possibleRoots)
        {
            if (File.Exists(Path.Combine(root, "plugins.json")))
            {
                return root;
            }
        }

        return null;
    }

    /// <summary>
    /// Determine the dist path based on environment
    /// </summary>
    private string DetermineDistPath()
    {
        // Priority 1: Environment variable override
        var envDist = Environment.GetEnvironmentVariable("VAPOURSYNTH_DIST");
        if (!string.IsNullOrEmpty(envDist) && Directory.Exists(envDist))
        {
            _logger.LogDebug("Using dist path from environment: {Path}", envDist);
            return envDist;
        }

        // Priority 2: Project root's dist folder (development)
        if (_projectRoot != null)
        {
            var projectDist = Path.Combine(_projectRoot, "dist");
            if (Directory.Exists(projectDist))
            {
                _logger.LogDebug("Using dist path from project root: {Path}", projectDist);
                return projectDist;
            }
        }

        // Priority 3: Alongside application (deployed)
        var appDist = Path.Combine(_appDirectory, "dist");
        if (Directory.Exists(appDist))
        {
            _logger.LogDebug("Using dist path alongside app: {Path}", appDist);
            return appDist;
        }

        // Priority 4: App directory itself contains dependencies
        if (File.Exists(Path.Combine(_appDirectory, "ffmpeg.exe")) ||
            File.Exists(Path.Combine(_appDirectory, "VSPipe.exe")))
        {
            _logger.LogDebug("Using app directory as dist path: {Path}", _appDirectory);
            return _appDirectory;
        }

        // Fallback: project root dist (may not exist yet)
        var fallback = _projectRoot != null
            ? Path.Combine(_projectRoot, "dist")
            : Path.Combine(_appDirectory, "dist");

        _logger.LogDebug("Using fallback dist path: {Path}", fallback);
        return fallback;
    }

    /// <summary>
    /// Find an executable in multiple locations
    /// </summary>
    public string? FindExecutable(string name, params string[] additionalSearchPaths)
    {
        var searchPaths = new List<string>();

        // Add additional paths first (highest priority)
        searchPaths.AddRange(additionalSearchPaths.Where(p => !string.IsNullOrEmpty(p)));

        // Add standard locations
        searchPaths.Add(_appDirectory);
        searchPaths.Add(_distPath);

        // Add subdirectories in dist
        var distSubdirs = new[] { "ffmpeg", "ffmpeg/bin", "vapoursynth", "python", "plugins" };
        foreach (var subdir in distSubdirs)
        {
            searchPaths.Add(Path.Combine(_distPath, subdir));
        }

        // Search each path
        foreach (var searchPath in searchPaths.Where(Directory.Exists))
        {
            var fullPath = Path.Combine(searchPath, name);
            if (File.Exists(fullPath))
            {
                _logger.LogDebug("Found {Executable} at: {Path}", name, fullPath);
                return fullPath;
            }
        }

        // Check PATH environment variable
        var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];
        foreach (var dir in pathDirs)
        {
            try
            {
                var fullPath = Path.Combine(dir, name);
                if (File.Exists(fullPath))
                {
                    _logger.LogDebug("Found {Executable} in PATH: {Path}", name, fullPath);
                    return fullPath;
                }
            }
            catch
            {
                // Invalid path in PATH variable, skip
            }
        }

        _logger.LogDebug("Executable not found: {Name}", name);
        return null;
    }

    private string? FindFFmpeg()
    {
        return FindExecutable("ffmpeg.exe");
    }

    private string? FindFFprobe()
    {
        return FindExecutable("ffprobe.exe");
    }

    private string? FindVSPipe()
    {
        // VSPipe is typically in the vapoursynth directory
        var vsDir = Path.Combine(_distPath, "vapoursynth");
        return FindExecutable("VSPipe.exe", vsDir);
    }

    private string? FindPythonPath()
    {
        var pythonDir = Path.Combine(_distPath, "python");
        if (Directory.Exists(pythonDir) && File.Exists(Path.Combine(pythonDir, "python.exe")))
        {
            return pythonDir;
        }

        // Check if python.exe is in app directory
        if (File.Exists(Path.Combine(_appDirectory, "python.exe")))
        {
            return _appDirectory;
        }

        return null;
    }

    /// <summary>
    /// Refresh cached paths (call after installing dependencies)
    /// </summary>
    public void RefreshPaths()
    {
        _ffmpegPath = null;
        _ffprobePath = null;
        _vspipePath = null;
        _pythonPath = null;

        _logger.LogInformation("Path cache cleared - paths will be re-resolved on next access");
    }

    /// <summary>
    /// Get environment variables for running VapourSynth processes
    /// </summary>
    public IDictionary<string, string> GetVapourSynthEnvironment()
    {
        var env = new Dictionary<string, string>();

        var existingPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        var pythonPath = PythonPath ?? Path.Combine(_distPath, "python");
        var vsPath = VapourSynthPath;
        var pluginsPath = PluginsPath;

        env["PATH"] = $"{pythonPath};{vsPath};{pluginsPath};{existingPath}";
        env["PYTHONPATH"] = vsPath;
        env["PYTHONHOME"] = pythonPath;

        return env;
    }
}

/// <summary>
/// Interface for path resolution service
/// </summary>
public interface IPathResolver
{
    string AppDirectory { get; }
    string? ProjectRoot { get; }
    string DistPath { get; }
    string? FFmpegPath { get; }
    string? FFprobePath { get; }
    string? VSPipePath { get; }
    string? PythonPath { get; }
    string VapourSynthPath { get; }
    string PluginsPath { get; }
    bool IsFFmpegAvailable { get; }
    bool IsVapourSynthAvailable { get; }
    bool IsPythonAvailable { get; }

    string? FindExecutable(string name, params string[] additionalSearchPaths);
    void RefreshPaths();
    IDictionary<string, string> GetVapourSynthEnvironment();
}
