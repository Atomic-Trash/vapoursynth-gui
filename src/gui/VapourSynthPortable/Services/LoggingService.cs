using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace VapourSynthPortable.Services;

/// <summary>
/// Centralized logging service using Serilog with file and UI sinks.
/// </summary>
public static class LoggingService
{
    private static readonly object _lock = new();
    private static bool _initialized;
    private static ILoggerFactory? _loggerFactory;
    private static readonly ObservableCollection<LogEntry> _logEntries = [];
    private static readonly MemorySink _memorySink = new();

    /// <summary>
    /// Observable collection of log entries for UI binding.
    /// </summary>
    public static ObservableCollection<LogEntry> LogEntries => _logEntries;

    /// <summary>
    /// Maximum number of log entries to keep in memory for UI display.
    /// </summary>
    public static int MaxLogEntries { get; set; } = 1000;

    /// <summary>
    /// Gets the log directory path.
    /// </summary>
    public static string LogDirectory { get; private set; } = "";

    /// <summary>
    /// Initializes the logging infrastructure. Call once at app startup.
    /// </summary>
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized) return;

            // Setup log directory
            LogDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VapourSynthStudio",
                "logs");

            Directory.CreateDirectory(LogDirectory);

            var logFilePath = Path.Combine(LogDirectory, "vapoursynth-studio-.log");

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Debug(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    shared: true)
                .WriteTo.Sink(_memorySink)
                .CreateLogger();

            // Create ILoggerFactory for DI
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(Log.Logger, dispose: false);
            });

            _initialized = true;

            // Log startup
            var logger = GetLogger<App>();
            logger.LogInformation("Logging initialized. Log directory: {LogDirectory}", LogDirectory);
        }
    }

    /// <summary>
    /// Gets a typed logger for dependency injection.
    /// </summary>
    public static ILogger<T> GetLogger<T>()
    {
        EnsureInitialized();
        return _loggerFactory!.CreateLogger<T>();
    }

    /// <summary>
    /// Gets a logger by category name.
    /// </summary>
    public static ILogger GetLogger(string categoryName)
    {
        EnsureInitialized();
        return _loggerFactory!.CreateLogger(categoryName);
    }

    /// <summary>
    /// Gets the ILoggerFactory for DI registration.
    /// </summary>
    public static ILoggerFactory GetLoggerFactory()
    {
        EnsureInitialized();
        return _loggerFactory!;
    }

    /// <summary>
    /// Clears log entries from the UI (doesn't affect file logs).
    /// </summary>
    public static void ClearLogEntries()
    {
        Application.Current?.Dispatcher.Invoke(() => _logEntries.Clear());
    }

    /// <summary>
    /// Shuts down logging. Call on application exit.
    /// </summary>
    public static void Shutdown()
    {
        lock (_lock)
        {
            if (!_initialized) return;

            Log.Information("Logging shutting down");
            Log.CloseAndFlush();
            _loggerFactory?.Dispose();
            _initialized = false;
        }
    }

    private static void EnsureInitialized()
    {
        if (!_initialized)
        {
            Initialize();
        }
    }

    /// <summary>
    /// Opens the log directory in Explorer.
    /// </summary>
    public static void OpenLogDirectory()
    {
        if (Directory.Exists(LogDirectory))
        {
            System.Diagnostics.Process.Start("explorer.exe", LogDirectory);
        }
    }

    /// <summary>
    /// Custom Serilog sink that feeds logs to the UI.
    /// </summary>
    private class MemorySink : ILogEventSink
    {
        private readonly MessageTemplateTextFormatter _formatter = new("[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

        public void Emit(LogEvent logEvent)
        {
            using var writer = new StringWriter();
            _formatter.Format(logEvent, writer);
            var message = writer.ToString().TrimEnd();

            var entry = new LogEntry
            {
                Timestamp = logEvent.Timestamp.LocalDateTime,
                Level = logEvent.Level switch
                {
                    LogEventLevel.Verbose => LogLevel.Trace,
                    LogEventLevel.Debug => LogLevel.Debug,
                    LogEventLevel.Information => LogLevel.Information,
                    LogEventLevel.Warning => LogLevel.Warning,
                    LogEventLevel.Error => LogLevel.Error,
                    LogEventLevel.Fatal => LogLevel.Critical,
                    _ => LogLevel.Information
                },
                Source = logEvent.Properties.TryGetValue("SourceContext", out var source)
                    ? source.ToString().Trim('"')
                    : "Unknown",
                Message = logEvent.RenderMessage(),
                FormattedMessage = message,
                Exception = logEvent.Exception
            };

            // Dispatch to UI thread
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                _logEntries.Add(entry);

                // Trim old entries if over limit
                while (_logEntries.Count > MaxLogEntries)
                {
                    _logEntries.RemoveAt(0);
                }
            });
        }
    }
}

/// <summary>
/// Represents a single log entry for UI display.
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; init; }
    public LogLevel Level { get; init; }
    public string Source { get; init; } = "";
    public string Message { get; init; } = "";
    public string FormattedMessage { get; init; } = "";
    public Exception? Exception { get; init; }

    public string LevelShort => Level switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Critical => "CRT",
        _ => "???"
    };

    public string TimestampShort => Timestamp.ToString("HH:mm:ss.fff");
}
