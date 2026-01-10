using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace VapourSynthPortable.Services;

/// <summary>
/// Telemetry report containing aggregated metrics and events.
/// </summary>
public class TelemetryReport
{
    public DateTime GeneratedAt { get; init; } = DateTime.Now;
    public TimeSpan Uptime { get; init; }
    public int TotalEvents { get; init; }
    public int TotalExceptions { get; init; }
    public Dictionary<string, int> EventCounts { get; init; } = [];
    public Dictionary<string, double> MetricAverages { get; init; } = [];
    public List<TelemetryEvent> RecentEvents { get; init; } = [];
}

/// <summary>
/// Represents a single telemetry event.
/// </summary>
public class TelemetryEvent
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string Name { get; init; } = "";
    public Dictionary<string, string>? Properties { get; init; }
    public bool IsException { get; init; }
}

/// <summary>
/// Represents a metric measurement.
/// </summary>
public class TelemetryMetric
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string Name { get; init; } = "";
    public double Value { get; init; }
}

/// <summary>
/// Interface for telemetry collection.
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Tracks a named event with optional properties.
    /// </summary>
    void TrackEvent(string eventName, Dictionary<string, string>? properties = null);

    /// <summary>
    /// Tracks a metric value.
    /// </summary>
    void TrackMetric(string metricName, double value);

    /// <summary>
    /// Tracks an exception with optional properties.
    /// </summary>
    void TrackException(Exception exception, Dictionary<string, string>? properties = null);

    /// <summary>
    /// Starts tracking an operation's duration. Call Dispose() on the returned object to complete tracking.
    /// </summary>
    IDisposable TrackOperation(string operationName);

    /// <summary>
    /// Generates a report of collected telemetry.
    /// </summary>
    TelemetryReport GenerateReport();

    /// <summary>
    /// Clears all collected telemetry data.
    /// </summary>
    void Clear();
}

/// <summary>
/// Local-only telemetry service for tracking app events and performance.
/// No data is sent externally - all telemetry is stored locally.
/// </summary>
public class TelemetryService : ITelemetryService
{
    private readonly ILogger<TelemetryService> _logger;
    private readonly ConcurrentQueue<TelemetryEvent> _events = new();
    private readonly ConcurrentQueue<TelemetryMetric> _metrics = new();
    private readonly ConcurrentDictionary<string, int> _eventCounts = new();
    private readonly ConcurrentDictionary<string, List<double>> _metricValues = new();
    private readonly DateTime _startTime = DateTime.Now;
    private readonly string _telemetryDir;
    private int _exceptionCount;

    private const int MaxEvents = 1000;
    private const int MaxMetrics = 10000;

    public TelemetryService(ILogger<TelemetryService>? logger = null)
    {
        _logger = logger ?? LoggingService.GetLogger<TelemetryService>();

        _telemetryDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VapourSynthStudio",
            "telemetry");

        Directory.CreateDirectory(_telemetryDir);
    }

    /// <inheritdoc/>
    public void TrackEvent(string eventName, Dictionary<string, string>? properties = null)
    {
        var evt = new TelemetryEvent
        {
            Name = eventName,
            Properties = properties
        };

        _events.Enqueue(evt);
        _eventCounts.AddOrUpdate(eventName, 1, (_, count) => count + 1);

        // Trim if too many events
        while (_events.Count > MaxEvents)
        {
            _events.TryDequeue(out _);
        }

        _logger.LogDebug("Telemetry event: {EventName}", eventName);
    }

    /// <inheritdoc/>
    public void TrackMetric(string metricName, double value)
    {
        var metric = new TelemetryMetric
        {
            Name = metricName,
            Value = value
        };

        _metrics.Enqueue(metric);
        _metricValues.AddOrUpdate(
            metricName,
            _ => [value],
            (_, list) =>
            {
                list.Add(value);
                if (list.Count > 100) list.RemoveAt(0); // Keep last 100 values per metric
                return list;
            });

        // Trim if too many metrics
        while (_metrics.Count > MaxMetrics)
        {
            _metrics.TryDequeue(out _);
        }
    }

    /// <inheritdoc/>
    public void TrackException(Exception exception, Dictionary<string, string>? properties = null)
    {
        Interlocked.Increment(ref _exceptionCount);

        var props = properties ?? [];
        props["ExceptionType"] = exception.GetType().Name;
        props["Message"] = exception.Message;

        var evt = new TelemetryEvent
        {
            Name = "Exception",
            Properties = props,
            IsException = true
        };

        _events.Enqueue(evt);
        _eventCounts.AddOrUpdate("Exception", 1, (_, count) => count + 1);

        _logger.LogDebug("Telemetry exception tracked: {ExceptionType}", exception.GetType().Name);
    }

    /// <inheritdoc/>
    public IDisposable TrackOperation(string operationName)
    {
        return new OperationTracker(this, operationName);
    }

    /// <inheritdoc/>
    public TelemetryReport GenerateReport()
    {
        var metricAverages = new Dictionary<string, double>();
        foreach (var kvp in _metricValues)
        {
            if (kvp.Value.Count > 0)
            {
                metricAverages[kvp.Key] = kvp.Value.Average();
            }
        }

        return new TelemetryReport
        {
            Uptime = DateTime.Now - _startTime,
            TotalEvents = _events.Count,
            TotalExceptions = _exceptionCount,
            EventCounts = new Dictionary<string, int>(_eventCounts),
            MetricAverages = metricAverages,
            RecentEvents = _events.TakeLast(50).ToList()
        };
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _events.Clear();
        _metrics.Clear();
        _eventCounts.Clear();
        _metricValues.Clear();
        _exceptionCount = 0;
    }

    /// <summary>
    /// Saves the current telemetry report to a file.
    /// </summary>
    public void SaveReport()
    {
        try
        {
            var report = GenerateReport();
            var filePath = Path.Combine(_telemetryDir, $"telemetry_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json, Encoding.UTF8);

            _logger.LogInformation("Telemetry report saved to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save telemetry report");
        }
    }

    /// <summary>
    /// Tracks the duration of an operation.
    /// </summary>
    private class OperationTracker : IDisposable
    {
        private readonly TelemetryService _service;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;

        public OperationTracker(TelemetryService service, string operationName)
        {
            _service = service;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();

            _service.TrackEvent($"{operationName}.Started");
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _service.TrackMetric($"{_operationName}.Duration", _stopwatch.Elapsed.TotalMilliseconds);
            _service.TrackEvent($"{_operationName}.Completed", new Dictionary<string, string>
            {
                ["DurationMs"] = _stopwatch.Elapsed.TotalMilliseconds.ToString("F2")
            });
        }
    }
}
