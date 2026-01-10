using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace VapourSynthPortable.Services;

/// <summary>
/// Service for extracting audio waveform data from media files using FFmpeg
/// </summary>
public class AudioWaveformService : IDisposable
{
    private static readonly ILogger<AudioWaveformService> _logger = LoggingService.GetLogger<AudioWaveformService>();

    // Cached compiled regex for duration parsing
    private static readonly Regex DurationRegex = new(@"Duration:\s*(\d+):(\d+):(\d+)\.(\d+)", RegexOptions.Compiled);

    private readonly string _ffmpegPath;
    private readonly ConcurrentDictionary<string, WaveformData> _cache = new();
    private readonly SemaphoreSlim _extractionSemaphore = new(2); // Limit concurrent extractions
    private bool _disposed;

    public AudioWaveformService()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var distPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "..", "dist"));
        _ffmpegPath = FindFFmpegPath(distPath);

        _logger.LogInformation("AudioWaveformService initialized. FFmpeg: {FFmpegPath}", _ffmpegPath);
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

        var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? [];
        foreach (var dir in pathDirs)
        {
            var fullPath = Path.Combine(dir, "ffmpeg.exe");
            if (File.Exists(fullPath)) return fullPath;
        }

        return "ffmpeg.exe";
    }

    /// <summary>
    /// Extract waveform data from an audio/video file
    /// </summary>
    /// <param name="filePath">Path to media file</param>
    /// <param name="samplesPerSecond">Target samples per second (default 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Waveform data with peak values</returns>
    public async Task<WaveformData?> ExtractWaveformAsync(
        string filePath,
        int samplesPerSecond = 100,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return null;

        var cacheKey = $"{filePath}|{samplesPerSecond}";

        // Check cache
        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        await _extractionSemaphore.WaitAsync(ct);
        try
        {
            // Double-check cache after acquiring semaphore
            if (_cache.TryGetValue(cacheKey, out cached))
                return cached;

            var waveform = await ExtractWaveformInternalAsync(filePath, samplesPerSecond, ct);

            if (waveform != null)
            {
                _cache[cacheKey] = waveform;
                _logger.LogInformation("Extracted waveform from {File}: {Samples} samples, {Duration:F2}s",
                    Path.GetFileName(filePath), waveform.Samples.Length, waveform.Duration);
            }

            return waveform;
        }
        finally
        {
            _extractionSemaphore.Release();
        }
    }

    private async Task<WaveformData?> ExtractWaveformInternalAsync(
        string filePath,
        int samplesPerSecond,
        CancellationToken ct)
    {
        try
        {
            // First, get the audio duration using ffprobe-style query
            var duration = await GetAudioDurationAsync(filePath, ct);
            if (duration <= 0)
            {
                _logger.LogWarning("Could not determine audio duration for: {File}", filePath);
                return null;
            }

            // Calculate target sample count
            int targetSamples = (int)(duration * samplesPerSecond);
            targetSamples = Math.Max(100, Math.Min(targetSamples, 10000)); // Limit to reasonable range

            // Extract audio as raw PCM, downsampled
            // We use a low sample rate to get approximate peaks
            int sampleRate = samplesPerSecond * 10; // 10 raw samples per output sample

            var tempFile = Path.Combine(Path.GetTempPath(), $"waveform_{Guid.NewGuid():N}.raw");

            try
            {
                // Extract mono audio as signed 16-bit PCM
                var args = $"-i \"{filePath}\" -vn -ac 1 -ar {sampleRate} -f s16le -y \"{tempFile}\"";

                var startInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                // Read stderr for progress/errors
                var stderrTask = process.StandardError.ReadToEndAsync();

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 second timeout

                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    try { process.Kill(); } catch { }
                    throw;
                }

                if (!File.Exists(tempFile) || new FileInfo(tempFile).Length == 0)
                {
                    var stderr = await stderrTask;
                    _logger.LogWarning("FFmpeg waveform extraction failed: {Error}", stderr);
                    return null;
                }

                // Read and process the raw PCM data
                var rawData = await File.ReadAllBytesAsync(tempFile, ct);
                var samples = ProcessRawPcmData(rawData, sampleRate, samplesPerSecond);

                return new WaveformData
                {
                    FilePath = filePath,
                    Duration = duration,
                    SampleRate = samplesPerSecond,
                    Samples = samples
                };
            }
            finally
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract waveform from: {File}", filePath);
            return null;
        }
    }

    private async Task<double> GetAudioDurationAsync(string filePath, CancellationToken ct)
    {
        try
        {
            // Use FFmpeg to get duration
            var args = $"-i \"{filePath}\" -hide_banner";

            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var stderr = await process.StandardError.ReadToEndAsync();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(); } catch { }
            }

            // Parse duration from stderr (format: Duration: HH:MM:SS.ms)
            var durationMatch = DurationRegex.Match(stderr);

            if (durationMatch.Success)
            {
                int hours = int.Parse(durationMatch.Groups[1].Value);
                int minutes = int.Parse(durationMatch.Groups[2].Value);
                int seconds = int.Parse(durationMatch.Groups[3].Value);
                int centiseconds = int.Parse(durationMatch.Groups[4].Value);

                return hours * 3600 + minutes * 60 + seconds + centiseconds / 100.0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get duration for: {File}", filePath);
        }

        return 0;
    }

    /// <summary>
    /// Process raw PCM data into peak samples
    /// </summary>
    private static WaveformSample[] ProcessRawPcmData(byte[] rawData, int inputSampleRate, int outputSampleRate)
    {
        // Convert bytes to 16-bit samples
        int sampleCount = rawData.Length / 2;
        var pcmSamples = new short[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            pcmSamples[i] = BitConverter.ToInt16(rawData, i * 2);
        }

        // Calculate how many input samples per output sample
        int samplesPerBucket = inputSampleRate / outputSampleRate;
        int outputCount = sampleCount / samplesPerBucket;

        if (outputCount == 0)
            return [];

        var result = new WaveformSample[outputCount];

        for (int i = 0; i < outputCount; i++)
        {
            int startIdx = i * samplesPerBucket;
            int endIdx = Math.Min(startIdx + samplesPerBucket, sampleCount);

            short min = short.MaxValue;
            short max = short.MinValue;
            long sumSquares = 0;
            int count = 0;

            for (int j = startIdx; j < endIdx; j++)
            {
                short sample = pcmSamples[j];
                min = Math.Min(min, sample);
                max = Math.Max(max, sample);
                sumSquares += sample * sample;
                count++;
            }

            float peakMin = min / 32768f;
            float peakMax = max / 32768f;
            float rms = count > 0 ? (float)Math.Sqrt(sumSquares / (double)count) / 32768f : 0;

            result[i] = new WaveformSample
            {
                Min = peakMin,
                Max = peakMax,
                Rms = rms
            };
        }

        return result;
    }

    /// <summary>
    /// Get waveform as simple float array (for compatibility with existing control)
    /// </summary>
    public async Task<float[]?> ExtractWaveformAsFloatArrayAsync(
        string filePath,
        int targetSamples = 1000,
        CancellationToken ct = default)
    {
        // Calculate samples per second based on duration
        var duration = await GetAudioDurationAsync(filePath, ct);
        if (duration <= 0)
            return null;

        int samplesPerSecond = Math.Max(1, (int)(targetSamples / duration));
        var waveform = await ExtractWaveformAsync(filePath, samplesPerSecond, ct);

        if (waveform == null)
            return null;

        // Convert to float array using peak values
        var result = new float[waveform.Samples.Length];
        for (int i = 0; i < waveform.Samples.Length; i++)
        {
            // Use the average of min/max peak as the sample value
            var sample = waveform.Samples[i];
            result[i] = (sample.Max + sample.Min) / 2f;
        }

        return result;
    }

    /// <summary>
    /// Extract stereo waveform data from an audio/video file
    /// </summary>
    public async Task<StereoWaveformData?> ExtractStereoWaveformAsync(
        string filePath,
        int samplesPerSecond = 100,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return null;

        var cacheKey = $"{filePath}|stereo|{samplesPerSecond}";

        await _extractionSemaphore.WaitAsync(ct);
        try
        {
            var duration = await GetAudioDurationAsync(filePath, ct);
            if (duration <= 0)
            {
                _logger.LogWarning("Could not determine audio duration for stereo: {File}", filePath);
                return null;
            }

            int sampleRate = samplesPerSecond * 10;
            var tempFileLeft = Path.Combine(Path.GetTempPath(), $"waveform_L_{Guid.NewGuid():N}.raw");
            var tempFileRight = Path.Combine(Path.GetTempPath(), $"waveform_R_{Guid.NewGuid():N}.raw");

            try
            {
                // Extract left channel
                var argsLeft = $"-i \"{filePath}\" -vn -af \"pan=mono|c0=c0\" -ar {sampleRate} -f s16le -y \"{tempFileLeft}\"";
                await RunFFmpegAsync(argsLeft, ct);

                // Extract right channel
                var argsRight = $"-i \"{filePath}\" -vn -af \"pan=mono|c0=c1\" -ar {sampleRate} -f s16le -y \"{tempFileRight}\"";
                await RunFFmpegAsync(argsRight, ct);

                WaveformSample[] leftSamples = [];
                WaveformSample[] rightSamples = [];
                int channelCount = 2;

                if (File.Exists(tempFileLeft) && new FileInfo(tempFileLeft).Length > 0)
                {
                    var rawLeft = await File.ReadAllBytesAsync(tempFileLeft, ct);
                    leftSamples = ProcessRawPcmData(rawLeft, sampleRate, samplesPerSecond);
                }

                if (File.Exists(tempFileRight) && new FileInfo(tempFileRight).Length > 0)
                {
                    var rawRight = await File.ReadAllBytesAsync(tempFileRight, ct);
                    rightSamples = ProcessRawPcmData(rawRight, sampleRate, samplesPerSecond);
                }
                else
                {
                    // Mono source - copy left to right
                    rightSamples = leftSamples;
                    channelCount = 1;
                }

                var result = new StereoWaveformData
                {
                    FilePath = filePath,
                    Duration = duration,
                    SampleRate = samplesPerSecond,
                    LeftChannel = leftSamples,
                    RightChannel = rightSamples,
                    ChannelCount = channelCount
                };

                _logger.LogInformation("Extracted stereo waveform from {File}: L={Left} R={Right} samples, peak={Peak:F2}dB",
                    Path.GetFileName(filePath), leftSamples.Length, rightSamples.Length, result.MaxPeakDb);

                return result;
            }
            finally
            {
                try { if (File.Exists(tempFileLeft)) File.Delete(tempFileLeft); } catch { }
                try { if (File.Exists(tempFileRight)) File.Delete(tempFileRight); } catch { }
            }
        }
        finally
        {
            _extractionSemaphore.Release();
        }
    }

    private async Task RunFFmpegAsync(string args, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(); } catch { }
            throw;
        }
    }

    /// <summary>
    /// Get audio analysis info (peak levels, clipping, etc.)
    /// </summary>
    public async Task<AudioAnalysis?> AnalyzeAudioAsync(string filePath, CancellationToken ct = default)
    {
        var stereo = await ExtractStereoWaveformAsync(filePath, 100, ct);
        if (stereo == null)
            return null;

        return new AudioAnalysis
        {
            FilePath = filePath,
            Duration = stereo.Duration,
            ChannelCount = stereo.ChannelCount,
            PeakLevel = stereo.MaxPeak,
            PeakLevelDb = stereo.MaxPeakDb,
            ClippingSamples = stereo.ClippingCount,
            HasClipping = stereo.ClippingCount > 0,
            LeftPeak = stereo.LeftChannel.Length > 0 ? stereo.LeftChannel.Max(s => s.AbsolutePeak) : 0,
            RightPeak = stereo.RightChannel.Length > 0 ? stereo.RightChannel.Max(s => s.AbsolutePeak) : 0
        };
    }

    /// <summary>
    /// Clear the waveform cache
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
        _logger.LogInformation("Waveform cache cleared");
    }

    /// <summary>
    /// Clear cache for a specific file
    /// </summary>
    public void ClearCacheForFile(string filePath)
    {
        var keysToRemove = _cache.Keys.Where(k => k.StartsWith(filePath + "|")).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _extractionSemaphore.Dispose();
    }
}

#region Models

/// <summary>
/// Display mode for audio waveform visualization
/// </summary>
public enum WaveformDisplayMode
{
    /// <summary>
    /// Display as single mono waveform (mixed from stereo if applicable)
    /// </summary>
    Mono,

    /// <summary>
    /// Display as separate left and right channels
    /// </summary>
    Stereo
}

/// <summary>
/// Represents extracted waveform data for an audio file
/// </summary>
public class WaveformData
{
    public required string FilePath { get; init; }
    public double Duration { get; init; }
    public int SampleRate { get; init; }
    public required WaveformSample[] Samples { get; init; }

    /// <summary>
    /// Get samples for a specific time range
    /// </summary>
    public WaveformSample[] GetSamplesInRange(double startTime, double endTime)
    {
        if (Samples.Length == 0 || Duration <= 0)
            return [];

        int startIdx = Math.Max(0, (int)(startTime / Duration * Samples.Length));
        int endIdx = Math.Min(Samples.Length, (int)(endTime / Duration * Samples.Length));

        if (endIdx <= startIdx)
            return [];

        var result = new WaveformSample[endIdx - startIdx];
        Array.Copy(Samples, startIdx, result, 0, result.Length);
        return result;
    }

    /// <summary>
    /// Get resampled waveform for display at a specific width
    /// </summary>
    public WaveformSample[] GetResampledForWidth(int targetWidth, double startTime = 0, double endTime = -1)
    {
        if (endTime < 0) endTime = Duration;

        var rangeSamples = GetSamplesInRange(startTime, endTime);
        if (rangeSamples.Length == 0)
            return [];

        if (rangeSamples.Length <= targetWidth)
            return rangeSamples;

        // Downsample by finding min/max/rms in each bucket
        var result = new WaveformSample[targetWidth];
        double samplesPerPixel = rangeSamples.Length / (double)targetWidth;

        for (int i = 0; i < targetWidth; i++)
        {
            int startIdx = (int)(i * samplesPerPixel);
            int endIdx = Math.Min(rangeSamples.Length, (int)((i + 1) * samplesPerPixel));

            float min = float.MaxValue;
            float max = float.MinValue;
            float rmsSum = 0;
            int count = 0;

            for (int j = startIdx; j < endIdx; j++)
            {
                min = Math.Min(min, rangeSamples[j].Min);
                max = Math.Max(max, rangeSamples[j].Max);
                rmsSum += rangeSamples[j].Rms * rangeSamples[j].Rms;
                count++;
            }

            result[i] = new WaveformSample
            {
                Min = min == float.MaxValue ? 0 : min,
                Max = max == float.MinValue ? 0 : max,
                Rms = count > 0 ? (float)Math.Sqrt(rmsSum / count) : 0
            };
        }

        return result;
    }
}

/// <summary>
/// Represents a single waveform sample with peak and RMS values
/// </summary>
public struct WaveformSample
{
    public float Min { get; init; }
    public float Max { get; init; }
    public float Rms { get; init; }

    /// <summary>
    /// Peak-to-peak amplitude
    /// </summary>
    public float PeakToPeak => Max - Min;

    /// <summary>
    /// Absolute peak value
    /// </summary>
    public float AbsolutePeak => Math.Max(Math.Abs(Min), Math.Abs(Max));

    /// <summary>
    /// Indicates if this sample clips (peak at or above 1.0)
    /// </summary>
    public bool IsClipping => AbsolutePeak >= 0.99f;

    /// <summary>
    /// Peak level in decibels
    /// </summary>
    public float PeakDb => AbsolutePeak > 0 ? 20f * (float)Math.Log10(AbsolutePeak) : -96f;
}

/// <summary>
/// Stereo waveform data with separate left and right channels
/// </summary>
public class StereoWaveformData
{
    public required string FilePath { get; init; }
    public double Duration { get; init; }
    public int SampleRate { get; init; }
    public required WaveformSample[] LeftChannel { get; init; }
    public required WaveformSample[] RightChannel { get; init; }
    public int ChannelCount { get; init; }

    /// <summary>
    /// Maximum peak level across both channels
    /// </summary>
    public float MaxPeak => Math.Max(
        LeftChannel.Length > 0 ? LeftChannel.Max(s => s.AbsolutePeak) : 0,
        RightChannel.Length > 0 ? RightChannel.Max(s => s.AbsolutePeak) : 0);

    /// <summary>
    /// Maximum peak level in dB
    /// </summary>
    public float MaxPeakDb => MaxPeak > 0 ? 20f * (float)Math.Log10(MaxPeak) : -96f;

    /// <summary>
    /// Count of samples that clip
    /// </summary>
    public int ClippingCount =>
        LeftChannel.Count(s => s.IsClipping) + RightChannel.Count(s => s.IsClipping);

    /// <summary>
    /// Get mono mix of both channels
    /// </summary>
    public WaveformSample[] GetMonoMix()
    {
        if (LeftChannel.Length == 0) return RightChannel;
        if (RightChannel.Length == 0) return LeftChannel;

        var mono = new WaveformSample[LeftChannel.Length];
        for (int i = 0; i < LeftChannel.Length; i++)
        {
            mono[i] = new WaveformSample
            {
                Min = (LeftChannel[i].Min + RightChannel[i].Min) / 2f,
                Max = (LeftChannel[i].Max + RightChannel[i].Max) / 2f,
                Rms = (float)Math.Sqrt((LeftChannel[i].Rms * LeftChannel[i].Rms +
                                        RightChannel[i].Rms * RightChannel[i].Rms) / 2f)
            };
        }
        return mono;
    }

    /// <summary>
    /// Get resampled stereo waveform for display
    /// </summary>
    public (WaveformSample[] left, WaveformSample[] right) GetResampledForWidth(
        int targetWidth, double startTime = 0, double endTime = -1)
    {
        if (endTime < 0) endTime = Duration;

        var left = ResampleChannel(LeftChannel, targetWidth, startTime, endTime);
        var right = ResampleChannel(RightChannel, targetWidth, startTime, endTime);

        return (left, right);
    }

    private WaveformSample[] ResampleChannel(WaveformSample[] channel, int targetWidth, double startTime, double endTime)
    {
        if (channel.Length == 0 || Duration <= 0)
            return [];

        int startIdx = Math.Max(0, (int)(startTime / Duration * channel.Length));
        int endIdx = Math.Min(channel.Length, (int)(endTime / Duration * channel.Length));

        if (endIdx <= startIdx)
            return [];

        int rangeLength = endIdx - startIdx;
        if (rangeLength <= targetWidth)
        {
            var result = new WaveformSample[rangeLength];
            Array.Copy(channel, startIdx, result, 0, rangeLength);
            return result;
        }

        var resampled = new WaveformSample[targetWidth];
        double samplesPerPixel = rangeLength / (double)targetWidth;

        for (int i = 0; i < targetWidth; i++)
        {
            int bucketStart = startIdx + (int)(i * samplesPerPixel);
            int bucketEnd = Math.Min(startIdx + rangeLength, startIdx + (int)((i + 1) * samplesPerPixel));

            float min = float.MaxValue;
            float max = float.MinValue;
            float rmsSum = 0;
            int count = 0;

            for (int j = bucketStart; j < bucketEnd; j++)
            {
                min = Math.Min(min, channel[j].Min);
                max = Math.Max(max, channel[j].Max);
                rmsSum += channel[j].Rms * channel[j].Rms;
                count++;
            }

            resampled[i] = new WaveformSample
            {
                Min = min == float.MaxValue ? 0 : min,
                Max = max == float.MinValue ? 0 : max,
                Rms = count > 0 ? (float)Math.Sqrt(rmsSum / count) : 0
            };
        }

        return resampled;
    }
}

/// <summary>
/// Audio analysis results
/// </summary>
public class AudioAnalysis
{
    public required string FilePath { get; init; }
    public double Duration { get; init; }
    public int ChannelCount { get; init; }
    public float PeakLevel { get; init; }
    public float PeakLevelDb { get; init; }
    public int ClippingSamples { get; init; }
    public bool HasClipping { get; init; }
    public float LeftPeak { get; init; }
    public float RightPeak { get; init; }

    public float LeftPeakDb => LeftPeak > 0 ? 20f * (float)Math.Log10(LeftPeak) : -96f;
    public float RightPeakDb => RightPeak > 0 ? 20f * (float)Math.Log10(RightPeak) : -96f;

    /// <summary>
    /// Headroom in dB (how much below 0dB the peak is)
    /// </summary>
    public float HeadroomDb => -PeakLevelDb;

    /// <summary>
    /// Suggested gain adjustment to normalize to -1dB
    /// </summary>
    public float SuggestedGainDb => -1f - PeakLevelDb;
}

#endregion
