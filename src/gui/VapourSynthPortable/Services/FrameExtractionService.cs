using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

public class FrameExtractionService
{
    private static readonly ILogger<FrameExtractionService> _logger = LoggingService.GetLogger<FrameExtractionService>();

    // Cached compiled regex patterns for VSPipe output parsing
    private static readonly Regex WidthRegex = new(@"Width:\s*(\d+)", RegexOptions.Compiled);
    private static readonly Regex HeightRegex = new(@"Height:\s*(\d+)", RegexOptions.Compiled);
    private static readonly Regex FramesRegex = new(@"Frames:\s*(\d+)", RegexOptions.Compiled);
    private static readonly Regex FpsRegex = new(@"FPS:\s*(\d+)/(\d+)", RegexOptions.Compiled);
    private static readonly Regex SimpleFpsRegex = new(@"FPS:\s*([\d.]+)", RegexOptions.Compiled);
    private static readonly Regex FormatRegex = new(@"Format Name:\s*(\w+)", RegexOptions.Compiled);
    private static readonly Regex ColorFamilyRegex = new(@"Color Family:\s*(\w+)", RegexOptions.Compiled);
    private static readonly Regex BitsRegex = new(@"Bits:\s*(\d+)", RegexOptions.Compiled);

    private readonly string _projectRoot;
    private readonly string _vspipePath;
    private readonly string _pythonPath;
    private readonly string _vapourSynthPath;
    private Process? _currentProcess;

    public FrameExtractionService()
    {
        _projectRoot = FindProjectRoot(AppDomain.CurrentDomain.BaseDirectory) ?? AppDomain.CurrentDomain.BaseDirectory;
        _vspipePath = Path.Combine(_projectRoot, "dist", "vapoursynth", "VSPipe.exe");
        _pythonPath = Path.Combine(_projectRoot, "dist", "python");
        _vapourSynthPath = Path.Combine(_projectRoot, "dist", "vapoursynth");

        _logger.LogInformation("FrameExtractionService initialized. VSPipe: {VSPipePath}, Available: {IsAvailable}",
            _vspipePath, IsAvailable);
    }

    public bool IsAvailable => File.Exists(_vspipePath);

    private string? FindProjectRoot(string startDir)
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

    public async Task<VideoInfo> GetVideoInfoAsync(string scriptPath, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting video info for script: {ScriptPath}", scriptPath);

        if (!IsAvailable)
        {
            _logger.LogError("VSPipe.exe not found at: {VSPipePath}", _vspipePath);
            throw new FileNotFoundException("VSPipe.exe not found. Please build the distribution first.", _vspipePath);
        }

        var startInfo = CreateProcessStartInfo("-i", scriptPath);
        var output = new StringBuilder();
        var error = new StringBuilder();

        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) error.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await WaitForProcessAsync(process, ct);

        if (process.ExitCode != 0)
        {
            _logger.LogError("VSPipe failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
            throw new InvalidOperationException($"VSPipe failed: {error}");
        }

        var info = ParseVideoInfo(output.ToString());
        _logger.LogInformation("Video info: {Width}x{Height}, {FrameCount} frames, {Fps:F3} fps",
            info.Width, info.Height, info.FrameCount, info.Fps);
        return info;
    }

    public async Task<byte[]> ExtractFrameAsync(string scriptPath, int frameNumber, VideoInfo videoInfo, CancellationToken ct = default)
    {
        _logger.LogDebug("Extracting frame {FrameNumber} from: {ScriptPath}", frameNumber, scriptPath);

        if (!IsAvailable)
        {
            _logger.LogError("VSPipe.exe not found at: {VSPipePath}", _vspipePath);
            throw new FileNotFoundException("VSPipe.exe not found.", _vspipePath);
        }

        // Create a wrapper script that converts to RGB24 for consistent output
        var wrapperScript = CreatePreviewScript(scriptPath, frameNumber);
        var tempScriptPath = Path.Combine(Path.GetTempPath(), $"vs_preview_{Guid.NewGuid():N}.vpy");

        try
        {
            await File.WriteAllTextAsync(tempScriptPath, wrapperScript, ct);

            var startInfo = CreateProcessStartInfo("-c", "y4m", "-o", "-", tempScriptPath);
            startInfo.RedirectStandardOutput = true;

            using var process = new Process { StartInfo = startInfo };
            _currentProcess = process;

            var frameData = new MemoryStream();
            var errorOutput = new StringBuilder();

            process.ErrorDataReceived += (s, e) => { if (e.Data != null) errorOutput.AppendLine(e.Data); };

            process.Start();
            process.BeginErrorReadLine();

            // Read raw frame data from stdout
            await process.StandardOutput.BaseStream.CopyToAsync(frameData, ct);
            await WaitForProcessAsync(process, ct);

            _currentProcess = null;

            if (process.ExitCode != 0)
            {
                _logger.LogError("Frame extraction failed with exit code {ExitCode}: {Error}",
                    process.ExitCode, errorOutput);
                throw new InvalidOperationException($"Frame extraction failed: {errorOutput}");
            }

            _logger.LogDebug("Frame {FrameNumber} extracted, {Size} bytes", frameNumber, frameData.Length);
            return frameData.ToArray();
        }
        finally
        {
            try { File.Delete(tempScriptPath); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temp script: {TempPath}", tempScriptPath);
            }
        }
    }

    public void Cancel()
    {
        try
        {
            if (_currentProcess != null && !_currentProcess.HasExited)
            {
                _logger.LogInformation("Cancelling frame extraction");
                _currentProcess.Kill();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error cancelling frame extraction");
        }
    }

    private ProcessStartInfo CreateProcessStartInfo(params string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _vspipePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _projectRoot
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        // Set up environment for portable VapourSynth
        var existingPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        startInfo.EnvironmentVariables["PATH"] = $"{_pythonPath};{_vapourSynthPath};{existingPath}";
        startInfo.EnvironmentVariables["PYTHONPATH"] = _vapourSynthPath;
        startInfo.EnvironmentVariables["PYTHONHOME"] = _pythonPath;

        return startInfo;
    }

    private async Task WaitForProcessAsync(Process process, CancellationToken ct)
    {
        while (!process.HasExited)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(50, ct);
        }
    }

    private VideoInfo ParseVideoInfo(string output)
    {
        var info = new VideoInfo();

        // Parse VSPipe -i output
        // Example output:
        // Width: 1920
        // Height: 1080
        // Frames: 1000
        // FPS: 24000/1001 (23.976 fps)
        // Format Name: YUV420P8
        // Color Family: YUV
        // Bits: 8

        var widthMatch = WidthRegex.Match(output);
        if (widthMatch.Success) info.Width = int.Parse(widthMatch.Groups[1].Value);

        var heightMatch = HeightRegex.Match(output);
        if (heightMatch.Success) info.Height = int.Parse(heightMatch.Groups[1].Value);

        var framesMatch = FramesRegex.Match(output);
        if (framesMatch.Success) info.FrameCount = int.Parse(framesMatch.Groups[1].Value);

        // FPS can be "24000/1001 (23.976 fps)" or just "24"
        var fpsMatch = FpsRegex.Match(output);
        if (fpsMatch.Success)
        {
            var num = double.Parse(fpsMatch.Groups[1].Value);
            var den = double.Parse(fpsMatch.Groups[2].Value);
            info.Fps = num / den;
        }
        else
        {
            var simpleFpsMatch = SimpleFpsRegex.Match(output);
            if (simpleFpsMatch.Success) info.Fps = double.Parse(simpleFpsMatch.Groups[1].Value);
        }

        var formatMatch = FormatRegex.Match(output);
        if (formatMatch.Success) info.Format = formatMatch.Groups[1].Value;

        var colorMatch = ColorFamilyRegex.Match(output);
        if (colorMatch.Success) info.ColorFamily = colorMatch.Groups[1].Value;

        var bitsMatch = BitsRegex.Match(output);
        if (bitsMatch.Success) info.BitsPerSample = int.Parse(bitsMatch.Groups[1].Value);

        return info;
    }

    private string CreatePreviewScript(string originalScriptPath, int frameNumber)
    {
        // Create wrapper that loads the original script and outputs a single RGB frame
        var escapedPath = originalScriptPath.Replace("\\", "\\\\");
        return $@"
import vapoursynth as vs
import importlib.util

# Load the original script
spec = importlib.util.spec_from_file_location('preview_script', r'{escapedPath}')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)

# Get the output clip
core = vs.core
clip = vs.get_output(0)

# Trim to single frame
clip = clip[{frameNumber}]

# Convert to RGB24 for easy display
clip = clip.resize.Bicubic(format=vs.RGB24, matrix_in_s='709')

clip.set_output()
";
    }
}
