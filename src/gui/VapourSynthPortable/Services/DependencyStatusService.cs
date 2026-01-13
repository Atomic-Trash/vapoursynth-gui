using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services.LibMpv;

namespace VapourSynthPortable.Services;

/// <summary>
/// Service for checking and reporting dependency availability
/// </summary>
public class DependencyStatusService : IDependencyStatusService
{
    private static readonly ILogger<DependencyStatusService> _logger = LoggingService.GetLogger<DependencyStatusService>();

    // Cached compiled regex patterns for version parsing
    private static readonly Regex VSVersionRegex = new(@"R(\d+)", RegexOptions.Compiled);
    private static readonly Regex VSAltVersionRegex = new(@"VapourSynth\s+(\S+)", RegexOptions.Compiled);
    private static readonly Regex FFmpegVersionRegex = new(@"ffmpeg version (\S+)", RegexOptions.Compiled);
    private static readonly Regex PythonVersionRegex = new(@"Python (\S+)", RegexOptions.Compiled);

    private readonly string _projectRoot;
    private readonly string _distPath;
    private DependencyStatusReport _currentStatus;

    public event EventHandler<DependencyStatusReport>? StatusChanged;

    public DependencyStatusReport CurrentStatus => _currentStatus;
    public bool IsReady => _currentStatus.AllRequiredAvailable;

    public DependencyStatusService()
    {
        _projectRoot = FindProjectRoot(AppDomain.CurrentDomain.BaseDirectory)
            ?? AppDomain.CurrentDomain.BaseDirectory;
        _distPath = Path.Combine(_projectRoot, "dist");

        // Initialize with unknown status
        _currentStatus = new DependencyStatusReport
        {
            VapourSynth = DependencyStatus.Unavailable("VapourSynth", "Not checked yet"),
            FFmpeg = DependencyStatus.Unavailable("FFmpeg", "Not checked yet"),
            Python = DependencyStatus.Unavailable("Python", "Not checked yet"),
            LibMpv = DependencyStatus.Unavailable("libmpv", "Not checked yet")
        };

        _logger.LogDebug("DependencyStatusService initialized. Project root: {ProjectRoot}", _projectRoot);
    }

    private static string? FindProjectRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        for (int i = 0; i < 10 && dir != null; i++)
        {
            if (File.Exists(Path.Combine(dir.FullName, "plugins.json")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    /// <summary>
    /// Check all dependencies and update status
    /// </summary>
    public async Task<DependencyStatusReport> CheckDependenciesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking all dependencies...");

        var vsTask = CheckVapourSynthAsync(cancellationToken);
        var ffmpegTask = CheckFFmpegAsync(cancellationToken);
        var pythonTask = CheckPythonAsync(cancellationToken);
        var libmpvStatus = CheckLibMpv(); // Synchronous - just checks file existence

        await Task.WhenAll(vsTask, ffmpegTask, pythonTask);

        var newStatus = new DependencyStatusReport
        {
            VapourSynth = await vsTask,
            FFmpeg = await ffmpegTask,
            Python = await pythonTask,
            LibMpv = libmpvStatus,
            CheckedAt = DateTime.UtcNow
        };

        _currentStatus = newStatus;
        StatusChanged?.Invoke(this, newStatus);

        _logger.LogInformation(
            "Dependency check complete. VS: {VS}, FFmpeg: {FFmpeg}, Python: {Python}, libmpv: {LibMpv}",
            newStatus.VapourSynth.IsAvailable,
            newStatus.FFmpeg.IsAvailable,
            newStatus.Python.IsAvailable,
            newStatus.LibMpv.IsAvailable);

        return newStatus;
    }

    /// <summary>
    /// Check if libmpv is available for video playback
    /// </summary>
    private DependencyStatus CheckLibMpv()
    {
        if (MpvPlayer.IsLibraryAvailable)
        {
            _logger.LogInformation("libmpv available for video playback");
            return DependencyStatus.Available("libmpv", "2.x", MpvPlayer.LibraryPath);
        }
        else
        {
            _logger.LogWarning("libmpv not found - video playback will be unavailable");
            return DependencyStatus.Unavailable("libmpv",
                "libmpv-2.dll not found. Run scripts/util/install-mpv.ps1 to install.");
        }
    }

    /// <summary>
    /// Check a specific dependency
    /// </summary>
    public async Task<Result<DependencyStatus>> CheckDependencyAsync(string dependencyName, CancellationToken cancellationToken = default)
    {
        return dependencyName.ToLowerInvariant() switch
        {
            "vapoursynth" or "vs" => Result<DependencyStatus>.Success(await CheckVapourSynthAsync(cancellationToken)),
            "ffmpeg" => Result<DependencyStatus>.Success(await CheckFFmpegAsync(cancellationToken)),
            "python" => Result<DependencyStatus>.Success(await CheckPythonAsync(cancellationToken)),
            "libmpv" or "mpv" => Result<DependencyStatus>.Success(CheckLibMpv()),
            _ => Result<DependencyStatus>.Failure($"Unknown dependency: {dependencyName}")
        };
    }

    private async Task<DependencyStatus> CheckVapourSynthAsync(CancellationToken cancellationToken)
    {
        var vspipePath = Path.Combine(_distPath, "vapoursynth", "VSPipe.exe");

        if (!File.Exists(vspipePath))
        {
            _logger.LogWarning("VSPipe not found at: {Path}", vspipePath);
            return DependencyStatus.Unavailable("VapourSynth", $"VSPipe.exe not found at {vspipePath}");
        }

        try
        {
            var version = await GetVapourSynthVersionAsync(vspipePath, cancellationToken);
            _logger.LogInformation("VapourSynth available. Version: {Version}", version);
            return DependencyStatus.Available("VapourSynth", version, vspipePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking VapourSynth");
            return DependencyStatus.Unavailable("VapourSynth", $"Error running VSPipe: {ex.Message}");
        }
    }

    private async Task<string?> GetVapourSynthVersionAsync(string vspipePath, CancellationToken cancellationToken)
    {
        try
        {
            var pythonPath = Path.Combine(_distPath, "python");
            var vsPath = Path.Combine(_distPath, "vapoursynth");

            var startInfo = new ProcessStartInfo
            {
                FileName = vspipePath,
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = _projectRoot
            };

            // Set up environment
            var existingPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            startInfo.EnvironmentVariables["PATH"] = $"{pythonPath};{vsPath};{existingPath}";
            startInfo.EnvironmentVariables["PYTHONPATH"] = vsPath;
            startInfo.EnvironmentVariables["PYTHONHOME"] = pythonPath;

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            await WaitForProcessAsync(process, cancellationToken, TimeSpan.FromSeconds(10));

            // Parse version from output like "VapourSynth Video Processing Library R68"
            var combinedOutput = output + error;
            var versionMatch = VSVersionRegex.Match(combinedOutput);
            if (versionMatch.Success)
            {
                return $"R{versionMatch.Groups[1].Value}";
            }

            // Try alternate pattern
            var altMatch = VSAltVersionRegex.Match(combinedOutput);
            if (altMatch.Success)
            {
                return altMatch.Groups[1].Value;
            }

            return "Unknown";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not determine VapourSynth version");
            return null;
        }
    }

    private async Task<DependencyStatus> CheckFFmpegAsync(CancellationToken cancellationToken)
    {
        var ffmpegPath = FindFFmpegPath();

        if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
        {
            _logger.LogWarning("FFmpeg not found");
            return DependencyStatus.Unavailable("FFmpeg", "ffmpeg.exe not found in dist/ffmpeg or PATH");
        }

        try
        {
            var version = await GetFFmpegVersionAsync(ffmpegPath, cancellationToken);
            _logger.LogInformation("FFmpeg available. Version: {Version}", version);
            return DependencyStatus.Available("FFmpeg", version, ffmpegPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking FFmpeg");
            return DependencyStatus.Unavailable("FFmpeg", $"Error running FFmpeg: {ex.Message}");
        }
    }

    private string? FindFFmpegPath()
    {
        var ffmpegDir = Path.Combine(_distPath, "ffmpeg");

        if (Directory.Exists(ffmpegDir))
        {
            var direct = Path.Combine(ffmpegDir, "ffmpeg.exe");
            if (File.Exists(direct)) return direct;

            var inBin = Path.Combine(ffmpegDir, "bin", "ffmpeg.exe");
            if (File.Exists(inBin)) return inBin;
        }

        // Check app directory
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var appDir = Path.Combine(basePath, "ffmpeg.exe");
        if (File.Exists(appDir)) return appDir;

        // Check PATH
        var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? [];
        foreach (var dir in pathDirs)
        {
            var path = Path.Combine(dir, "ffmpeg.exe");
            if (File.Exists(path)) return path;
        }

        return null;
    }

    private async Task<string?> GetFFmpegVersionAsync(string ffmpegPath, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = "-version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);

            await WaitForProcessAsync(process, cancellationToken, TimeSpan.FromSeconds(10));

            // Parse version from "ffmpeg version 6.1.1 ..."
            var versionMatch = FFmpegVersionRegex.Match(output);
            if (versionMatch.Success)
            {
                return versionMatch.Groups[1].Value;
            }

            return "Unknown";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not determine FFmpeg version");
            return null;
        }
    }

    private async Task<DependencyStatus> CheckPythonAsync(CancellationToken cancellationToken)
    {
        var pythonPath = Path.Combine(_distPath, "python", "python.exe");

        if (!File.Exists(pythonPath))
        {
            _logger.LogWarning("Python not found at: {Path}", pythonPath);
            return DependencyStatus.Unavailable("Python", $"python.exe not found at {pythonPath}");
        }

        try
        {
            var version = await GetPythonVersionAsync(pythonPath, cancellationToken);
            _logger.LogInformation("Python available. Version: {Version}", version);
            return DependencyStatus.Available("Python", version, pythonPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Python");
            return DependencyStatus.Unavailable("Python", $"Error running Python: {ex.Message}");
        }
    }

    private async Task<string?> GetPythonVersionAsync(string pythonPath, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            await WaitForProcessAsync(process, cancellationToken, TimeSpan.FromSeconds(10));

            // Parse version from "Python 3.12.1"
            var versionMatch = PythonVersionRegex.Match(output + error);
            if (versionMatch.Success)
            {
                return versionMatch.Groups[1].Value;
            }

            return "Unknown";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not determine Python version");
            return null;
        }
    }

    private static async Task WaitForProcessAsync(Process process, CancellationToken cancellationToken, TimeSpan timeout)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            while (!process.HasExited)
            {
                linkedCts.Token.ThrowIfCancellationRequested();
                await Task.Delay(50, linkedCts.Token);
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            try { process.Kill(); } catch (Exception ex) { _logger.LogDebug(ex, "Process already terminated during timeout"); }
            throw new TimeoutException($"Process did not complete within {timeout.TotalSeconds} seconds");
        }
    }
}
