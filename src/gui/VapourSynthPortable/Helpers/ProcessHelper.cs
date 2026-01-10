using System.Diagnostics;
using System.IO;

namespace VapourSynthPortable.Helpers;

/// <summary>
/// Helper class for creating and configuring processes with VapourSynth environment
/// </summary>
public static class ProcessHelper
{
    private static readonly Lazy<VapourSynthPaths> _paths = new(InitializePaths);

    /// <summary>
    /// Gets the common VapourSynth paths
    /// </summary>
    public static VapourSynthPaths Paths => _paths.Value;

    /// <summary>
    /// Creates a ProcessStartInfo configured for VapourSynth operations
    /// </summary>
    /// <param name="executable">The executable to run</param>
    /// <param name="arguments">Command line arguments</param>
    /// <param name="redirectOutput">Whether to redirect stdout</param>
    /// <param name="redirectError">Whether to redirect stderr</param>
    /// <returns>Configured ProcessStartInfo</returns>
    public static ProcessStartInfo CreateProcessStartInfo(
        string executable,
        string? arguments = null,
        bool redirectOutput = true,
        bool redirectError = true)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments ?? "",
            UseShellExecute = false,
            RedirectStandardOutput = redirectOutput,
            RedirectStandardError = redirectError,
            CreateNoWindow = true,
            WorkingDirectory = Paths.ProjectRoot
        };

        ConfigureEnvironment(startInfo);
        return startInfo;
    }

    /// <summary>
    /// Creates a ProcessStartInfo for VSPipe with arguments
    /// </summary>
    /// <param name="args">Arguments to pass to VSPipe</param>
    /// <returns>Configured ProcessStartInfo</returns>
    public static ProcessStartInfo CreateVSPipeProcess(params string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Paths.VSPipePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Paths.ProjectRoot
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        ConfigureEnvironment(startInfo);
        return startInfo;
    }

    /// <summary>
    /// Creates a ProcessStartInfo for FFmpeg with arguments
    /// </summary>
    /// <param name="arguments">FFmpeg command line arguments</param>
    /// <param name="redirectInput">Whether to redirect stdin (for piping)</param>
    /// <returns>Configured ProcessStartInfo</returns>
    public static ProcessStartInfo CreateFFmpegProcess(
        string arguments,
        bool redirectInput = false)
    {
        return new ProcessStartInfo
        {
            FileName = Paths.FFmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardInput = redirectInput,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Paths.ProjectRoot
        };
    }

    /// <summary>
    /// Creates a ProcessStartInfo for Python with arguments
    /// </summary>
    /// <param name="arguments">Python command line arguments</param>
    /// <returns>Configured ProcessStartInfo</returns>
    public static ProcessStartInfo CreatePythonProcess(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Paths.PythonExePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Paths.ProjectRoot
        };

        ConfigureEnvironment(startInfo);
        return startInfo;
    }

    /// <summary>
    /// Configures the environment variables for VapourSynth portability
    /// </summary>
    /// <param name="startInfo">ProcessStartInfo to configure</param>
    public static void ConfigureEnvironment(ProcessStartInfo startInfo)
    {
        var existingPath = Environment.GetEnvironmentVariable("PATH") ?? "";

        // Add VapourSynth paths to PATH
        startInfo.EnvironmentVariables["PATH"] =
            $"{Paths.PythonPath};{Paths.VapourSynthPath};{Paths.PluginsPath};{existingPath}";

        // Configure Python environment
        startInfo.EnvironmentVariables["PYTHONPATH"] = Paths.VapourSynthPath;
        startInfo.EnvironmentVariables["PYTHONHOME"] = Paths.PythonPath;
    }

    /// <summary>
    /// Runs a process asynchronously and returns when it exits
    /// </summary>
    /// <param name="process">The process to run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public static async Task WaitForProcessAsync(Process process, CancellationToken cancellationToken = default)
    {
        while (!process.HasExited)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(50, cancellationToken);
        }
    }

    /// <summary>
    /// Runs a process and captures its output
    /// </summary>
    /// <param name="startInfo">ProcessStartInfo for the process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (exitCode, stdout, stderr)</returns>
    public static async Task<(int ExitCode, string Output, string Error)> RunProcessAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken = default)
    {
        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var outputTask = startInfo.RedirectStandardOutput
            ? process.StandardOutput.ReadToEndAsync(cancellationToken)
            : Task.FromResult("");
        var errorTask = startInfo.RedirectStandardError
            ? process.StandardError.ReadToEndAsync(cancellationToken)
            : Task.FromResult("");

        await WaitForProcessAsync(process, cancellationToken);

        return (process.ExitCode, await outputTask, await errorTask);
    }

    /// <summary>
    /// Finds the project root by searching for plugins.json
    /// </summary>
    /// <param name="startDir">Directory to start searching from</param>
    /// <param name="maxDepth">Maximum parent directories to search</param>
    /// <returns>Project root path or null if not found</returns>
    public static string? FindProjectRoot(string startDir, int maxDepth = 10)
    {
        var dir = new DirectoryInfo(startDir);
        for (int i = 0; i < maxDepth && dir != null; i++)
        {
            if (File.Exists(Path.Combine(dir.FullName, "plugins.json")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    /// <summary>
    /// Finds FFmpeg executable in the distribution
    /// </summary>
    /// <param name="distPath">Distribution path</param>
    /// <returns>Path to ffmpeg.exe</returns>
    public static string FindFFmpegPath(string distPath)
    {
        // Check common locations
        var locations = new[]
        {
            Path.Combine(distPath, "ffmpeg", "ffmpeg.exe"),
            Path.Combine(distPath, "ffmpeg", "bin", "ffmpeg.exe"),
            Path.Combine(distPath, "bin", "ffmpeg.exe"),
            "ffmpeg.exe" // System PATH
        };

        return locations.FirstOrDefault(File.Exists) ?? locations[0];
    }

    private static VapourSynthPaths InitializePaths()
    {
        var projectRoot = FindProjectRoot(AppDomain.CurrentDomain.BaseDirectory)
            ?? AppDomain.CurrentDomain.BaseDirectory;

        var distPath = Path.Combine(projectRoot, "dist");

        return new VapourSynthPaths
        {
            ProjectRoot = projectRoot,
            DistPath = distPath,
            PythonPath = Path.Combine(distPath, "python"),
            PythonExePath = Path.Combine(distPath, "python", "python.exe"),
            VapourSynthPath = Path.Combine(distPath, "vapoursynth"),
            VSPipePath = Path.Combine(distPath, "vapoursynth", "VSPipe.exe"),
            PluginsPath = Path.Combine(distPath, "plugins"),
            FFmpegPath = FindFFmpegPath(distPath),
            ScriptsPath = Path.Combine(distPath, "scripts")
        };
    }
}

/// <summary>
/// Contains common paths for VapourSynth distribution
/// </summary>
public class VapourSynthPaths
{
    /// <summary>
    /// Root directory containing plugins.json
    /// </summary>
    public required string ProjectRoot { get; init; }

    /// <summary>
    /// Distribution directory (dist/)
    /// </summary>
    public required string DistPath { get; init; }

    /// <summary>
    /// Embedded Python directory
    /// </summary>
    public required string PythonPath { get; init; }

    /// <summary>
    /// Path to python.exe
    /// </summary>
    public required string PythonExePath { get; init; }

    /// <summary>
    /// VapourSynth core directory
    /// </summary>
    public required string VapourSynthPath { get; init; }

    /// <summary>
    /// Path to VSPipe.exe
    /// </summary>
    public required string VSPipePath { get; init; }

    /// <summary>
    /// Plugins directory (.dll files)
    /// </summary>
    public required string PluginsPath { get; init; }

    /// <summary>
    /// Path to ffmpeg.exe
    /// </summary>
    public required string FFmpegPath { get; init; }

    /// <summary>
    /// VapourSynth scripts directory (.vpy files)
    /// </summary>
    public required string ScriptsPath { get; init; }

    /// <summary>
    /// Checks if VSPipe is available
    /// </summary>
    public bool IsVSPipeAvailable => File.Exists(VSPipePath);

    /// <summary>
    /// Checks if FFmpeg is available
    /// </summary>
    public bool IsFFmpegAvailable => File.Exists(FFmpegPath);

    /// <summary>
    /// Checks if Python is available
    /// </summary>
    public bool IsPythonAvailable => File.Exists(PythonExePath);
}
