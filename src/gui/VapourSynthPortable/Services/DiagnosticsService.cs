using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace VapourSynthPortable.Services;

/// <summary>
/// Information about a loaded VapourSynth plugin.
/// </summary>
public class PluginInfo
{
    public string Name { get; init; } = "";
    public string FileName { get; init; } = "";
    public string Path { get; init; } = "";
    public string Version { get; init; } = "";
    public bool IsLoaded { get; init; }
}

/// <summary>
/// Information about the system GPU.
/// </summary>
public class GpuInfo
{
    public string Name { get; init; } = "";
    public string DriverVersion { get; init; } = "";
    public long AdapterRam { get; init; }
    public bool SupportsNvenc { get; init; }
    public bool SupportsAmf { get; init; }
    public bool SupportsQsv { get; init; }
}

/// <summary>
/// Complete system diagnostics report.
/// </summary>
public record DiagnosticsReport
{
    public DateTime GeneratedAt { get; init; } = DateTime.Now;
    public bool DotNetOk { get; init; }
    public string DotNetVersion { get; init; } = "";
    public bool VapourSynthOk { get; init; }
    public string VapourSynthVersion { get; init; } = "";
    public bool PythonOk { get; init; }
    public string PythonVersion { get; init; } = "";
    public bool FFmpegOk { get; init; }
    public string FFmpegVersion { get; init; } = "";
    public bool LibMpvOk { get; init; }
    public List<PluginInfo> Plugins { get; init; } = [];
    public GpuInfo? Gpu { get; init; }
    public long AvailableMemoryMb { get; init; }
    public string OsVersion { get; init; } = "";
    public List<string> Issues { get; init; } = [];
    public List<string> Recommendations { get; init; } = [];
}

/// <summary>
/// Interface for system diagnostics.
/// </summary>
public interface IDiagnosticsService
{
    /// <summary>
    /// Runs a full system diagnostic.
    /// </summary>
    Task<DiagnosticsReport> RunFullDiagnostics();

    /// <summary>
    /// Checks if VapourSynth is available.
    /// </summary>
    Task<bool> CheckVapourSynth();

    /// <summary>
    /// Checks if Python is available.
    /// </summary>
    Task<bool> CheckPython();

    /// <summary>
    /// Gets information about loaded plugins.
    /// </summary>
    Task<List<PluginInfo>> GetLoadedPlugins();

    /// <summary>
    /// Gets GPU information.
    /// </summary>
    Task<GpuInfo?> GetGpuInfo();
}

/// <summary>
/// Service for system health checks and diagnostics.
/// </summary>
public class DiagnosticsService : IDiagnosticsService
{
    private readonly ILogger<DiagnosticsService> _logger;
    private readonly string _basePath;

    public DiagnosticsService(ILogger<DiagnosticsService>? logger = null)
    {
        _logger = logger ?? LoggingService.GetLogger<DiagnosticsService>();
        _basePath = AppDomain.CurrentDomain.BaseDirectory;
    }

    /// <inheritdoc/>
    public async Task<DiagnosticsReport> RunFullDiagnostics()
    {
        _logger.LogInformation("Running full system diagnostics...");

        var report = new DiagnosticsReport
        {
            OsVersion = Environment.OSVersion.ToString(),
            DotNetVersion = RuntimeInformation.FrameworkDescription,
            DotNetOk = true
        };

        // Check VapourSynth
        report = report with { VapourSynthOk = await CheckVapourSynth() };
        if (report.VapourSynthOk)
        {
            report = report with { VapourSynthVersion = await GetVapourSynthVersion() };
        }

        // Check Python
        report = report with { PythonOk = await CheckPython() };
        if (report.PythonOk)
        {
            report = report with { PythonVersion = await GetPythonVersion() };
        }

        // Check FFmpeg
        report = report with { FFmpegOk = await CheckFFmpeg() };
        if (report.FFmpegOk)
        {
            report = report with { FFmpegVersion = await GetFFmpegVersion() };
        }

        // Check libmpv
        report = report with { LibMpvOk = CheckLibMpv() };

        // Get plugins
        report = report with { Plugins = await GetLoadedPlugins() };

        // Get GPU info
        report = report with { Gpu = await GetGpuInfo() };

        // Get available memory
        report = report with { AvailableMemoryMb = GetAvailableMemory() };

        // Generate issues and recommendations
        var issues = new List<string>();
        var recommendations = new List<string>();

        if (!report.VapourSynthOk)
        {
            issues.Add("VapourSynth not found or not working");
            recommendations.Add("Run Build-Portable.ps1 to set up VapourSynth");
        }

        if (!report.PythonOk)
        {
            issues.Add("Embedded Python not found");
            recommendations.Add("Run Build-Portable.ps1 to set up Python");
        }

        if (!report.FFmpegOk)
        {
            issues.Add("FFmpeg not found");
            recommendations.Add("FFmpeg is required for video encoding");
        }

        if (!report.LibMpvOk)
        {
            issues.Add("libmpv not found");
            recommendations.Add("Run install-mpv.ps1 for video preview support");
        }

        if (report.Gpu == null)
        {
            recommendations.Add("No GPU detected - hardware acceleration unavailable");
        }

        report = report with
        {
            Issues = issues,
            Recommendations = recommendations
        };

        _logger.LogInformation("Diagnostics complete. {IssueCount} issues found", issues.Count);
        return report;
    }

    /// <inheritdoc/>
    public async Task<bool> CheckVapourSynth()
    {
        var vspipePath = FindVspipe();
        if (string.IsNullOrEmpty(vspipePath) || !File.Exists(vspipePath))
        {
            return false;
        }

        try
        {
            var result = await RunProcessAsync(vspipePath, "--version");
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CheckPython()
    {
        var pythonPath = FindPython();
        if (string.IsNullOrEmpty(pythonPath) || !File.Exists(pythonPath))
        {
            return false;
        }

        try
        {
            var result = await RunProcessAsync(pythonPath, "--version");
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckFFmpeg()
    {
        var ffmpegPath = FindFFmpeg();
        if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
        {
            return false;
        }

        try
        {
            var result = await RunProcessAsync(ffmpegPath, "-version");
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private bool CheckLibMpv()
    {
        var mpvDir = FindMpvDir();
        if (string.IsNullOrEmpty(mpvDir) || !Directory.Exists(mpvDir))
        {
            return false;
        }

        return File.Exists(Path.Combine(mpvDir, "libmpv-2.dll")) ||
               File.Exists(Path.Combine(mpvDir, "mpv-2.dll"));
    }

    /// <inheritdoc/>
    public async Task<List<PluginInfo>> GetLoadedPlugins()
    {
        var plugins = new List<PluginInfo>();
        var pluginDir = FindPluginDir();

        if (string.IsNullOrEmpty(pluginDir) || !Directory.Exists(pluginDir))
        {
            return plugins;
        }

        foreach (var file in Directory.GetFiles(pluginDir, "*.dll"))
        {
            plugins.Add(new PluginInfo
            {
                Name = Path.GetFileNameWithoutExtension(file),
                FileName = Path.GetFileName(file),
                Path = file,
                IsLoaded = true // We can't easily check if VS has loaded it
            });
        }

        return await Task.FromResult(plugins);
    }

    /// <inheritdoc/>
    public async Task<GpuInfo?> GetGpuInfo()
    {
        try
        {
            var result = await RunProcessAsync("wmic", "path win32_VideoController get name,driverversion,adapterram /format:csv");

            if (result.ExitCode != 0 || string.IsNullOrEmpty(result.Output))
            {
                return null;
            }

            var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines.Skip(1)) // Skip header
            {
                var parts = line.Trim().Split(',');
                if (parts.Length >= 4)
                {
                    var name = parts.Length > 2 ? parts[2] : "";
                    var driverVersion = parts.Length > 1 ? parts[1] : "";
                    long.TryParse(parts.Length > 0 ? parts[0] : "0", out var adapterRam);

                    if (!string.IsNullOrEmpty(name))
                    {
                        return new GpuInfo
                        {
                            Name = name,
                            DriverVersion = driverVersion,
                            AdapterRam = adapterRam,
                            SupportsNvenc = name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase),
                            SupportsAmf = name.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                                          name.Contains("Radeon", StringComparison.OrdinalIgnoreCase),
                            SupportsQsv = name.Contains("Intel", StringComparison.OrdinalIgnoreCase)
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get GPU info");
        }

        return null;
    }

    private async Task<string> GetVapourSynthVersion()
    {
        var vspipePath = FindVspipe();
        if (string.IsNullOrEmpty(vspipePath)) return "";

        try
        {
            var result = await RunProcessAsync(vspipePath, "--version");
            return result.Output.Split('\n').FirstOrDefault()?.Trim() ?? "";
        }
        catch
        {
            return "";
        }
    }

    private async Task<string> GetPythonVersion()
    {
        var pythonPath = FindPython();
        if (string.IsNullOrEmpty(pythonPath)) return "";

        try
        {
            var result = await RunProcessAsync(pythonPath, "--version");
            return result.Output.Trim();
        }
        catch
        {
            return "";
        }
    }

    private async Task<string> GetFFmpegVersion()
    {
        var ffmpegPath = FindFFmpeg();
        if (string.IsNullOrEmpty(ffmpegPath)) return "";

        try
        {
            var result = await RunProcessAsync(ffmpegPath, "-version");
            return result.Output.Split('\n').FirstOrDefault()?.Trim() ?? "";
        }
        catch
        {
            return "";
        }
    }

    private long GetAvailableMemory()
    {
        try
        {
            using var counter = new PerformanceCounter("Memory", "Available MBytes");
            return (long)counter.NextValue();
        }
        catch
        {
            return 0;
        }
    }

    private string? FindVspipe()
    {
        var distPath = FindDistPath();
        if (distPath == null) return null;

        var vsDir = Path.Combine(distPath, "vapoursynth");
        var vspipe = Path.Combine(vsDir, "vspipe.exe");
        return File.Exists(vspipe) ? vspipe : null;
    }

    private string? FindPython()
    {
        var distPath = FindDistPath();
        if (distPath == null) return null;

        var python = Path.Combine(distPath, "python", "python.exe");
        return File.Exists(python) ? python : null;
    }

    private string? FindFFmpeg()
    {
        var distPath = FindDistPath();
        if (distPath == null) return null;

        var ffmpeg = Path.Combine(distPath, "ffmpeg", "ffmpeg.exe");
        if (File.Exists(ffmpeg)) return ffmpeg;

        ffmpeg = Path.Combine(distPath, "ffmpeg", "bin", "ffmpeg.exe");
        return File.Exists(ffmpeg) ? ffmpeg : null;
    }

    private string? FindMpvDir()
    {
        var distPath = FindDistPath();
        if (distPath == null) return null;

        var mpvDir = Path.Combine(distPath, "mpv");
        return Directory.Exists(mpvDir) ? mpvDir : null;
    }

    private string? FindPluginDir()
    {
        var distPath = FindDistPath();
        if (distPath == null) return null;

        var pluginDir = Path.Combine(distPath, "vapoursynth", "vapoursynth64", "plugins");
        return Directory.Exists(pluginDir) ? pluginDir : null;
    }

    private string? FindDistPath()
    {
        // Try to find dist directory relative to base path
        var dir = new DirectoryInfo(_basePath);
        for (int i = 0; i < 10 && dir != null; i++)
        {
            var distPath = Path.Combine(dir.FullName, "dist");
            if (Directory.Exists(distPath))
            {
                return distPath;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private async Task<(int ExitCode, string Output)> RunProcessAsync(string fileName, string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, output);
    }

    /// <summary>
    /// Generates a formatted diagnostic report string.
    /// </summary>
    public static string FormatReport(DiagnosticsReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("VapourSynth Studio - System Diagnostic Report");
        sb.AppendLine("═══════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"OS: {report.OsVersion}");
        sb.AppendLine();
        sb.AppendLine("Component          Status    Details");
        sb.AppendLine("─────────────────────────────────────────────────────");
        sb.AppendLine($".NET SDK           [{(report.DotNetOk ? "OK" : "!!")}]      {report.DotNetVersion}");
        sb.AppendLine($"VapourSynth        [{(report.VapourSynthOk ? "OK" : "!!")}]      {report.VapourSynthVersion}");
        sb.AppendLine($"Python             [{(report.PythonOk ? "OK" : "!!")}]      {report.PythonVersion}");
        sb.AppendLine($"FFmpeg             [{(report.FFmpegOk ? "OK" : "!!")}]      {report.FFmpegVersion}");
        sb.AppendLine($"libmpv             [{(report.LibMpvOk ? "OK" : "!!")}]      {(report.LibMpvOk ? "Found" : "Not found")}");
        sb.AppendLine($"GPU                [{(report.Gpu != null ? "OK" : "--")}]      {report.Gpu?.Name ?? "Not detected"}");
        sb.AppendLine($"Plugins            [{(report.Plugins.Count > 0 ? "OK" : "--")}]      {report.Plugins.Count} loaded");
        sb.AppendLine($"Memory             [OK]      {report.AvailableMemoryMb} MB available");

        if (report.Issues.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Issues Found:");
            sb.AppendLine("─────────────");
            foreach (var issue in report.Issues)
            {
                sb.AppendLine($"  - {issue}");
            }
        }

        if (report.Recommendations.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Recommendations:");
            sb.AppendLine("─────────────────");
            foreach (var rec in report.Recommendations)
            {
                sb.AppendLine($"  - {rec}");
            }
        }

        return sb.ToString();
    }
}
