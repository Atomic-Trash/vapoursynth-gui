using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace VapourSynthPortable.Services;

/// <summary>
/// Service for executing VapourSynth scripts and piping output to FFmpeg
/// </summary>
public class VapourSynthService
{
    private static readonly ILogger<VapourSynthService> _logger = LoggingService.GetLogger<VapourSynthService>();

    private readonly string _projectRoot;
    private readonly string _vspipePath;
    private readonly string _pythonPath;
    private readonly string _vapourSynthPath;
    private readonly string _ffmpegPath;
    private readonly string _pluginsPath;

    private Process? _vspipeProcess;
    private Process? _ffmpegProcess;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isProcessing;

    public event EventHandler<VapourSynthProgressEventArgs>? ProgressChanged;
    public event EventHandler<string>? LogMessage;
    public event EventHandler? ProcessingStarted;
    public event EventHandler<VapourSynthCompletedEventArgs>? ProcessingCompleted;

    public bool IsProcessing => _isProcessing;
    public bool IsAvailable => File.Exists(_vspipePath);

    public VapourSynthService()
    {
        _projectRoot = FindProjectRoot(AppDomain.CurrentDomain.BaseDirectory)
            ?? AppDomain.CurrentDomain.BaseDirectory;

        var distPath = Path.Combine(_projectRoot, "dist");
        _vspipePath = Path.Combine(distPath, "vapoursynth", "VSPipe.exe");
        _pythonPath = Path.Combine(distPath, "python");
        _vapourSynthPath = Path.Combine(distPath, "vapoursynth");
        _pluginsPath = Path.Combine(distPath, "plugins");
        _ffmpegPath = FindFFmpegPath(distPath);

        _logger.LogInformation(
            "VapourSynthService initialized. VSPipe: {VSPipePath}, FFmpeg: {FFmpegPath}, Available: {IsAvailable}",
            _vspipePath, _ffmpegPath, IsAvailable);
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

    private static string FindFFmpegPath(string distPath)
    {
        var ffmpegDir = Path.Combine(distPath, "ffmpeg");
        if (Directory.Exists(ffmpegDir))
        {
            var direct = Path.Combine(ffmpegDir, "ffmpeg.exe");
            if (File.Exists(direct)) return direct;

            var inBin = Path.Combine(ffmpegDir, "bin", "ffmpeg.exe");
            if (File.Exists(inBin)) return inBin;
        }

        // Fall back to PATH
        return "ffmpeg.exe";
    }

    /// <summary>
    /// Get information about a VapourSynth script
    /// </summary>
    public async Task<VapourSynthScriptInfo?> GetScriptInfoAsync(string scriptPath, CancellationToken ct = default)
    {
        if (!File.Exists(scriptPath))
        {
            _logger.LogError("Script file not found: {ScriptPath}", scriptPath);
            return null;
        }

        if (!IsAvailable)
        {
            _logger.LogError("VSPipe not found at: {VSPipePath}", _vspipePath);
            return null;
        }

        try
        {
            var startInfo = CreateVSPipeStartInfo("-i", scriptPath);
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
                _logger.LogError("VSPipe info failed: {Error}", error);
                return null;
            }

            return ParseScriptInfo(output.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get script info for: {ScriptPath}", scriptPath);
            return null;
        }
    }

    /// <summary>
    /// Execute a VapourSynth script and encode output with FFmpeg
    /// </summary>
    public async Task<bool> ProcessScriptAsync(
        string scriptPath,
        string outputPath,
        VapourSynthEncodingSettings? settings = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(scriptPath))
        {
            _logger.LogError("Script file not found: {ScriptPath}", scriptPath);
            LogMessage?.Invoke(this, $"Error: Script file not found: {scriptPath}");
            return false;
        }

        if (!IsAvailable)
        {
            _logger.LogError("VSPipe not found");
            LogMessage?.Invoke(this, "Error: VSPipe not found. Please build the distribution first.");
            return false;
        }

        settings ??= new VapourSynthEncodingSettings();
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _isProcessing = true;

        try
        {
            // Get script info for progress calculation
            var scriptInfo = await GetScriptInfoAsync(scriptPath, _cancellationTokenSource.Token);
            var totalFrames = scriptInfo?.FrameCount ?? 0;

            LogMessage?.Invoke(this, $"Processing: {Path.GetFileName(scriptPath)}");
            LogMessage?.Invoke(this, $"Output: {outputPath}");
            if (scriptInfo != null)
            {
                LogMessage?.Invoke(this, $"Source: {scriptInfo.Width}x{scriptInfo.Height}, {totalFrames} frames");
            }

            ProcessingStarted?.Invoke(this, EventArgs.Empty);

            // Create output directory if needed
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Run the pipeline
            var success = await RunPipelineAsync(scriptPath, outputPath, settings, totalFrames, _cancellationTokenSource.Token);

            var cancelled = _cancellationTokenSource.IsCancellationRequested;

            ProcessingCompleted?.Invoke(this, new VapourSynthCompletedEventArgs
            {
                Success = success && !cancelled,
                OutputPath = outputPath,
                Cancelled = cancelled,
                TotalFrames = totalFrames
            });

            if (success && !cancelled)
            {
                _logger.LogInformation("Processing completed: {OutputPath}", outputPath);
                LogMessage?.Invoke(this, "Processing completed successfully!");
            }
            else if (cancelled)
            {
                _logger.LogInformation("Processing cancelled");
                LogMessage?.Invoke(this, "Processing cancelled.");
                TryDeleteFile(outputPath);
            }
            else
            {
                _logger.LogError("Processing failed");
                LogMessage?.Invoke(this, "Processing failed.");
            }

            return success && !cancelled;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Processing cancelled");
            TryDeleteFile(outputPath);
            ProcessingCompleted?.Invoke(this, new VapourSynthCompletedEventArgs
            {
                Success = false,
                OutputPath = outputPath,
                Cancelled = true
            });
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing failed with exception");
            LogMessage?.Invoke(this, $"Error: {ex.Message}");
            ProcessingCompleted?.Invoke(this, new VapourSynthCompletedEventArgs
            {
                Success = false,
                OutputPath = outputPath,
                Cancelled = false,
                ErrorMessage = ex.Message
            });
            return false;
        }
        finally
        {
            _isProcessing = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private async Task<bool> RunPipelineAsync(
        string scriptPath,
        string outputPath,
        VapourSynthEncodingSettings settings,
        int totalFrames,
        CancellationToken ct)
    {
        // Build FFmpeg arguments
        var ffmpegArgs = BuildFFmpegArguments(settings, outputPath);

        _logger.LogDebug("VSPipe: {VSPipePath} \"{ScriptPath}\" -c y4m -", _vspipePath, scriptPath);
        _logger.LogDebug("FFmpeg: {FFmpegPath} {Args}", _ffmpegPath, ffmpegArgs);

        // Start VSPipe process
        var vspipeStartInfo = CreateVSPipeStartInfo("-c", "y4m", "-p", "-", scriptPath);
        vspipeStartInfo.RedirectStandardOutput = true;

        _vspipeProcess = new Process { StartInfo = vspipeStartInfo };

        // Start FFmpeg process
        var ffmpegStartInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = ffmpegArgs,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        _ffmpegProcess = new Process { StartInfo = ffmpegStartInfo };

        var errorOutput = new StringBuilder();
        var frameRegex = new Regex(@"Frame:\s*(\d+)\s*/\s*(\d+)", RegexOptions.Compiled);
        var currentFrame = 0;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Handle VSPipe stderr for progress
            _vspipeProcess.ErrorDataReceived += (s, e) =>
            {
                if (e.Data == null) return;

                // VSPipe outputs progress like "Frame: 100/1000"
                var match = frameRegex.Match(e.Data);
                if (match.Success)
                {
                    currentFrame = int.Parse(match.Groups[1].Value);
                    var total = int.Parse(match.Groups[2].Value);
                    if (totalFrames == 0) totalFrames = total;

                    var progress = totalFrames > 0 ? (double)currentFrame / totalFrames * 100 : 0;
                    var fps = stopwatch.Elapsed.TotalSeconds > 0
                        ? currentFrame / stopwatch.Elapsed.TotalSeconds
                        : 0;

                    ProgressChanged?.Invoke(this, new VapourSynthProgressEventArgs
                    {
                        CurrentFrame = currentFrame,
                        TotalFrames = totalFrames,
                        Progress = progress,
                        Fps = fps,
                        ElapsedTime = stopwatch.Elapsed
                    });
                }
                else if (e.Data.Contains("error", StringComparison.OrdinalIgnoreCase))
                {
                    LogMessage?.Invoke(this, $"VSPipe: {e.Data}");
                    errorOutput.AppendLine(e.Data);
                }
            };

            // Handle FFmpeg stderr
            _ffmpegProcess.ErrorDataReceived += (s, e) =>
            {
                if (e.Data == null) return;

                if (e.Data.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                    e.Data.Contains("warning", StringComparison.OrdinalIgnoreCase))
                {
                    LogMessage?.Invoke(this, $"FFmpeg: {e.Data}");
                }
            };

            // Start both processes
            _vspipeProcess.Start();
            _vspipeProcess.BeginErrorReadLine();

            _ffmpegProcess.Start();
            _ffmpegProcess.BeginErrorReadLine();

            // Pipe VSPipe stdout to FFmpeg stdin
            var pipeTask = Task.Run(async () =>
            {
                try
                {
                    await _vspipeProcess.StandardOutput.BaseStream.CopyToAsync(
                        _ffmpegProcess.StandardInput.BaseStream, ct);
                    _ffmpegProcess.StandardInput.Close();
                }
                catch (OperationCanceledException)
                {
                    // Expected on cancellation
                }
                catch (IOException)
                {
                    // Pipe broken, process likely terminated
                }
            }, ct);

            // Wait for both processes
            var vspipeTask = WaitForProcessAsync(_vspipeProcess, ct);
            var ffmpegTask = WaitForProcessAsync(_ffmpegProcess, ct);

            await Task.WhenAll(pipeTask, vspipeTask, ffmpegTask);

            stopwatch.Stop();

            // Report final progress
            ProgressChanged?.Invoke(this, new VapourSynthProgressEventArgs
            {
                CurrentFrame = totalFrames,
                TotalFrames = totalFrames,
                Progress = 100,
                Fps = totalFrames / stopwatch.Elapsed.TotalSeconds,
                ElapsedTime = stopwatch.Elapsed
            });

            var vspipeSuccess = _vspipeProcess.ExitCode == 0;
            var ffmpegSuccess = _ffmpegProcess.ExitCode == 0;

            if (!vspipeSuccess)
            {
                _logger.LogError("VSPipe failed with exit code {ExitCode}", _vspipeProcess.ExitCode);
                LogMessage?.Invoke(this, $"VSPipe failed with exit code {_vspipeProcess.ExitCode}");
            }

            if (!ffmpegSuccess)
            {
                _logger.LogError("FFmpeg failed with exit code {ExitCode}", _ffmpegProcess.ExitCode);
                LogMessage?.Invoke(this, $"FFmpeg failed with exit code {_ffmpegProcess.ExitCode}");
            }

            return vspipeSuccess && ffmpegSuccess;
        }
        finally
        {
            _vspipeProcess?.Dispose();
            _ffmpegProcess?.Dispose();
            _vspipeProcess = null;
            _ffmpegProcess = null;
        }
    }

    private string BuildFFmpegArguments(VapourSynthEncodingSettings settings, string outputPath)
    {
        var args = new List<string>
        {
            "-y",                    // Overwrite output
            "-i", "-"                // Input from pipe
        };

        // Video codec
        args.Add("-c:v");
        args.Add(settings.VideoCodec);

        // Codec-specific settings
        if (settings.VideoCodec == "libx264" || settings.VideoCodec == "libx265")
        {
            args.Add("-crf");
            args.Add(settings.Quality.ToString());
            args.Add("-preset");
            args.Add(settings.Preset);

            if (settings.VideoCodec == "libx265")
            {
                args.Add("-tag:v");
                args.Add("hvc1");
            }
        }
        else if (settings.VideoCodec == "h264_nvenc" || settings.VideoCodec == "hevc_nvenc")
        {
            args.Add("-preset");
            args.Add(settings.HardwarePreset);
            args.Add("-cq");
            args.Add(settings.Quality.ToString());
        }

        // Pixel format
        if (!string.IsNullOrEmpty(settings.PixelFormat))
        {
            args.Add("-pix_fmt");
            args.Add(settings.PixelFormat);
        }

        // Audio (none from VSPipe, but can add from original source later)
        args.Add("-an");

        // Container options
        if (outputPath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
        {
            args.Add("-movflags");
            args.Add("+faststart");
        }

        // Output path
        args.Add($"\"{outputPath}\"");

        return string.Join(" ", args);
    }

    private ProcessStartInfo CreateVSPipeStartInfo(params string[] args)
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
        startInfo.EnvironmentVariables["PATH"] = $"{_pythonPath};{_vapourSynthPath};{_pluginsPath};{existingPath}";
        startInfo.EnvironmentVariables["PYTHONPATH"] = _vapourSynthPath;
        startInfo.EnvironmentVariables["PYTHONHOME"] = _pythonPath;

        return startInfo;
    }

    private VapourSynthScriptInfo ParseScriptInfo(string output)
    {
        var info = new VapourSynthScriptInfo();

        var widthMatch = Regex.Match(output, @"Width:\s*(\d+)");
        if (widthMatch.Success) info.Width = int.Parse(widthMatch.Groups[1].Value);

        var heightMatch = Regex.Match(output, @"Height:\s*(\d+)");
        if (heightMatch.Success) info.Height = int.Parse(heightMatch.Groups[1].Value);

        var framesMatch = Regex.Match(output, @"Frames:\s*(\d+)");
        if (framesMatch.Success) info.FrameCount = int.Parse(framesMatch.Groups[1].Value);

        var fpsMatch = Regex.Match(output, @"FPS:\s*(\d+)/(\d+)");
        if (fpsMatch.Success)
        {
            var num = double.Parse(fpsMatch.Groups[1].Value);
            var den = double.Parse(fpsMatch.Groups[2].Value);
            info.Fps = num / den;
            info.FpsNum = (int)num;
            info.FpsDen = (int)den;
        }
        else
        {
            var simpleFpsMatch = Regex.Match(output, @"FPS:\s*([\d.]+)");
            if (simpleFpsMatch.Success) info.Fps = double.Parse(simpleFpsMatch.Groups[1].Value);
        }

        var formatMatch = Regex.Match(output, @"Format Name:\s*(\w+)");
        if (formatMatch.Success) info.Format = formatMatch.Groups[1].Value;

        var colorMatch = Regex.Match(output, @"Color Family:\s*(\w+)");
        if (colorMatch.Success) info.ColorFamily = colorMatch.Groups[1].Value;

        var bitsMatch = Regex.Match(output, @"Bits:\s*(\d+)");
        if (bitsMatch.Success) info.BitsPerSample = int.Parse(bitsMatch.Groups[1].Value);

        _logger.LogDebug("Parsed script info: {Width}x{Height}, {FrameCount} frames, {Fps:F3} fps, {Format}",
            info.Width, info.Height, info.FrameCount, info.Fps, info.Format);

        return info;
    }

    private async Task WaitForProcessAsync(Process process, CancellationToken ct)
    {
        while (!process.HasExited)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(50, ct);
        }
    }

    public void Cancel()
    {
        _logger.LogInformation("Cancelling VapourSynth processing");
        _cancellationTokenSource?.Cancel();

        try
        {
            if (_vspipeProcess != null && !_vspipeProcess.HasExited)
                _vspipeProcess.Kill();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error killing VSPipe process");
        }

        try
        {
            if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
                _ffmpegProcess.Kill();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error killing FFmpeg process");
        }
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file: {Path}", path);
        }
    }

    /// <summary>
    /// Validate a VapourSynth script without processing
    /// </summary>
    public async Task<VapourSynthValidationResult> ValidateScriptAsync(string scriptPath, CancellationToken ct = default)
    {
        var result = new VapourSynthValidationResult();

        if (!File.Exists(scriptPath))
        {
            result.IsValid = false;
            result.Errors.Add($"Script file not found: {scriptPath}");
            return result;
        }

        if (!IsAvailable)
        {
            result.IsValid = false;
            result.Errors.Add("VSPipe not found. Please build the distribution first.");
            return result;
        }

        try
        {
            var info = await GetScriptInfoAsync(scriptPath, ct);
            if (info != null)
            {
                result.IsValid = true;
                result.ScriptInfo = info;
            }
            else
            {
                result.IsValid = false;
                result.Errors.Add("Failed to parse script output");
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    /// <summary>
    /// List available VapourSynth plugins
    /// </summary>
    public async Task<List<VapourSynthPlugin>> GetPluginsAsync(CancellationToken ct = default)
    {
        var plugins = new List<VapourSynthPlugin>();

        if (!IsAvailable)
            return plugins;

        try
        {
            // Create a simple script to list plugins
            var scriptContent = @"
import vapoursynth as vs
core = vs.core

# Print all plugins
for plugin in core.plugins():
    print(f'PLUGIN:{plugin.namespace}|{plugin.name}|{plugin.identifier}')

# Create a dummy clip for output
clip = core.std.BlankClip()
clip.set_output()
";
            var tempScript = Path.Combine(Path.GetTempPath(), $"vs_plugins_{Guid.NewGuid():N}.vpy");
            await File.WriteAllTextAsync(tempScript, scriptContent, ct);

            try
            {
                var startInfo = CreateVSPipeStartInfo("-i", tempScript);
                var output = new StringBuilder();

                using var process = new Process { StartInfo = startInfo };
                process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };

                process.Start();
                process.BeginOutputReadLine();

                await WaitForProcessAsync(process, ct);

                // Parse plugin output
                foreach (var line in output.ToString().Split('\n'))
                {
                    if (line.StartsWith("PLUGIN:"))
                    {
                        var parts = line.Substring(7).Split('|');
                        if (parts.Length >= 3)
                        {
                            plugins.Add(new VapourSynthPlugin
                            {
                                Namespace = parts[0],
                                Name = parts[1],
                                Identifier = parts[2]
                            });
                        }
                    }
                }
            }
            finally
            {
                TryDeleteFile(tempScript);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get plugin list");
        }

        return plugins;
    }
}

#region Models

public class VapourSynthScriptInfo
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int FrameCount { get; set; }
    public double Fps { get; set; }
    public int FpsNum { get; set; }
    public int FpsDen { get; set; }
    public string Format { get; set; } = "";
    public string ColorFamily { get; set; } = "";
    public int BitsPerSample { get; set; }

    public string Resolution => $"{Width}x{Height}";
    public TimeSpan Duration => Fps > 0
        ? TimeSpan.FromSeconds(FrameCount / Fps)
        : TimeSpan.Zero;
}

public class VapourSynthEncodingSettings
{
    public string VideoCodec { get; set; } = "libx264";
    public int Quality { get; set; } = 18;
    public string Preset { get; set; } = "medium";
    public string HardwarePreset { get; set; } = "p4";
    public string PixelFormat { get; set; } = "yuv420p";
}

public class VapourSynthProgressEventArgs : EventArgs
{
    public int CurrentFrame { get; set; }
    public int TotalFrames { get; set; }
    public double Progress { get; set; }
    public double Fps { get; set; }
    public TimeSpan ElapsedTime { get; set; }

    public TimeSpan EstimatedTimeRemaining
    {
        get
        {
            if (Fps <= 0 || CurrentFrame <= 0) return TimeSpan.Zero;
            var remainingFrames = TotalFrames - CurrentFrame;
            return TimeSpan.FromSeconds(remainingFrames / Fps);
        }
    }
}

public class VapourSynthCompletedEventArgs : EventArgs
{
    public bool Success { get; set; }
    public bool Cancelled { get; set; }
    public string OutputPath { get; set; } = "";
    public string? ErrorMessage { get; set; }
    public int TotalFrames { get; set; }
}

public class VapourSynthValidationResult
{
    public bool IsValid { get; set; }
    public VapourSynthScriptInfo? ScriptInfo { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public class VapourSynthPlugin
{
    public string Namespace { get; set; } = "";
    public string Name { get; set; } = "";
    public string Identifier { get; set; } = "";
}

#endregion
