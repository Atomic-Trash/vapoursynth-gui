using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace VapourSynthPortable.Services;

/// <summary>
/// Captures unhandled exceptions and writes detailed crash reports to disk.
/// </summary>
public static class CrashReporter
{
    private static readonly object _lock = new();
    private static bool _initialized;
    private static string _crashDirectory = "";
    private static ILogger? _logger;

    /// <summary>
    /// Gets the directory where crash reports are saved.
    /// </summary>
    public static string CrashDirectory => _crashDirectory;

    /// <summary>
    /// Initializes the crash reporter. Call once at app startup after LoggingService.Initialize().
    /// </summary>
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized) return;

            _crashDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VapourSynthStudio",
                "crashes");

            Directory.CreateDirectory(_crashDirectory);

            _logger = LoggingService.GetLogger("CrashReporter");
            _initialized = true;

            _logger.LogInformation("CrashReporter initialized. Crash directory: {CrashDirectory}", _crashDirectory);
        }
    }

    /// <summary>
    /// Creates a crash report for an exception and saves it to disk.
    /// </summary>
    /// <param name="exception">The exception to report.</param>
    /// <param name="source">Where the exception originated (e.g., "DispatcherUnhandled", "AppDomain").</param>
    /// <param name="isTerminating">Whether the exception is terminating the application.</param>
    /// <returns>The path to the crash report file, or null if creation failed.</returns>
    public static string? CreateCrashReport(Exception exception, string source, bool isTerminating = false)
    {
        if (!_initialized)
        {
            Initialize();
        }

        try
        {
            var timestamp = DateTime.Now;
            var fileName = $"crash_{timestamp:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.txt";
            var filePath = Path.Combine(_crashDirectory, fileName);

            var report = BuildCrashReport(exception, source, isTerminating, timestamp);
            File.WriteAllText(filePath, report, Encoding.UTF8);

            _logger?.LogError(exception, "Crash report created: {FilePath}", filePath);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create crash report");
            return null;
        }
    }

    /// <summary>
    /// Shows a user-friendly error dialog with option to view the crash report.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="crashReportPath">Path to the crash report file.</param>
    public static void ShowCrashDialog(Exception exception, string? crashReportPath)
    {
        var message = new StringBuilder();
        message.AppendLine("An unexpected error occurred.");
        message.AppendLine();
        message.AppendLine($"Error: {exception.Message}");

        if (!string.IsNullOrEmpty(crashReportPath))
        {
            message.AppendLine();
            message.AppendLine($"A crash report has been saved to:");
            message.AppendLine(crashReportPath);
        }

        var result = MessageBox.Show(
            message.ToString(),
            "VapourSynth Studio - Error",
            crashReportPath != null ? MessageBoxButton.YesNo : MessageBoxButton.OK,
            MessageBoxImage.Error);

        if (result == MessageBoxResult.Yes && crashReportPath != null && File.Exists(crashReportPath))
        {
            OpenCrashReport(crashReportPath);
        }
    }

    /// <summary>
    /// Opens a crash report in the default text editor.
    /// </summary>
    public static void OpenCrashReport(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open crash report: {FilePath}", filePath);
        }
    }

    /// <summary>
    /// Opens the crash directory in Explorer.
    /// </summary>
    public static void OpenCrashDirectory()
    {
        if (Directory.Exists(_crashDirectory))
        {
            Process.Start("explorer.exe", _crashDirectory);
        }
    }

    /// <summary>
    /// Gets the count of crash reports in the crash directory.
    /// </summary>
    public static int GetCrashReportCount()
    {
        if (!Directory.Exists(_crashDirectory)) return 0;
        return Directory.GetFiles(_crashDirectory, "crash_*.txt").Length;
    }

    /// <summary>
    /// Cleans up old crash reports, keeping only the most recent ones.
    /// </summary>
    /// <param name="keepCount">Number of recent crash reports to keep.</param>
    public static void CleanupOldReports(int keepCount = 10)
    {
        if (!Directory.Exists(_crashDirectory)) return;

        try
        {
            var files = Directory.GetFiles(_crashDirectory, "crash_*.txt")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Skip(keepCount)
                .ToList();

            foreach (var file in files)
            {
                file.Delete();
                _logger?.LogInformation("Deleted old crash report: {FileName}", file.Name);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to cleanup old crash reports");
        }
    }

    private static string BuildCrashReport(Exception exception, string source, bool isTerminating, DateTime timestamp)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                    VAPOURSYNTH STUDIO CRASH REPORT                          ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();

        // Basic info
        sb.AppendLine("═══ CRASH INFORMATION ═══");
        sb.AppendLine($"Timestamp:     {timestamp:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"Source:        {source}");
        sb.AppendLine($"Is Terminating: {isTerminating}");
        sb.AppendLine();

        // Exception details
        sb.AppendLine("═══ EXCEPTION DETAILS ═══");
        AppendExceptionDetails(sb, exception, 0);
        sb.AppendLine();

        // Environment info
        sb.AppendLine("═══ ENVIRONMENT ═══");
        sb.AppendLine($"OS Version:    {Environment.OSVersion}");
        sb.AppendLine($"64-bit OS:     {Environment.Is64BitOperatingSystem}");
        sb.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");
        sb.AppendLine($".NET Version:  {Environment.Version}");
        sb.AppendLine($"CLR Version:   {RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"Machine Name:  {Environment.MachineName}");
        sb.AppendLine($"User Name:     {Environment.UserName}");
        sb.AppendLine($"Processors:    {Environment.ProcessorCount}");
        sb.AppendLine();

        // Process info
        sb.AppendLine("═══ PROCESS INFORMATION ═══");
        try
        {
            using var process = Process.GetCurrentProcess();
            sb.AppendLine($"Process ID:    {process.Id}");
            sb.AppendLine($"Process Name:  {process.ProcessName}");
            sb.AppendLine($"Working Set:   {process.WorkingSet64 / 1024 / 1024:N0} MB");
            sb.AppendLine($"Private Memory: {process.PrivateMemorySize64 / 1024 / 1024:N0} MB");
            sb.AppendLine($"Start Time:    {process.StartTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Total CPU Time: {process.TotalProcessorTime}");
            sb.AppendLine($"Thread Count:  {process.Threads.Count}");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"[Failed to get process info: {ex.Message}]");
        }
        sb.AppendLine();

        // Application info
        sb.AppendLine("═══ APPLICATION ═══");
        try
        {
            var assembly = Assembly.GetEntryAssembly();
            if (assembly != null)
            {
                sb.AppendLine($"Assembly:      {assembly.GetName().Name}");
                sb.AppendLine($"Version:       {assembly.GetName().Version}");
                sb.AppendLine($"Location:      {assembly.Location}");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"[Failed to get assembly info: {ex.Message}]");
        }
        sb.AppendLine();

        // Loaded assemblies
        sb.AppendLine("═══ LOADED ASSEMBLIES ═══");
        try
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()
                         .Where(a => !a.IsDynamic)
                         .OrderBy(a => a.GetName().Name))
            {
                sb.AppendLine($"  {asm.GetName().Name} v{asm.GetName().Version}");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"[Failed to get loaded assemblies: {ex.Message}]");
        }
        sb.AppendLine();

        // Recent log entries
        sb.AppendLine("═══ RECENT LOG ENTRIES ═══");
        try
        {
            var recentLogs = LoggingService.LogEntries
                .TakeLast(50)
                .ToList();

            if (recentLogs.Count > 0)
            {
                foreach (var entry in recentLogs)
                {
                    sb.AppendLine($"[{entry.TimestampShort}] [{entry.LevelShort}] [{entry.Source}] {entry.Message}");
                }
            }
            else
            {
                sb.AppendLine("  (No log entries available)");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"[Failed to get log entries: {ex.Message}]");
        }
        sb.AppendLine();

        // Footer
        sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        sb.AppendLine("Please include this crash report when reporting issues at:");
        sb.AppendLine("https://github.com/yourrepo/vapoursynth-studio/issues");
        sb.AppendLine("═══════════════════════════════════════════════════════════════════════════════");

        return sb.ToString();
    }

    private static void AppendExceptionDetails(StringBuilder sb, Exception exception, int depth)
    {
        var indent = new string(' ', depth * 2);

        sb.AppendLine($"{indent}Type:    {exception.GetType().FullName}");
        sb.AppendLine($"{indent}Message: {exception.Message}");
        sb.AppendLine($"{indent}Source:  {exception.Source}");

        if (!string.IsNullOrEmpty(exception.StackTrace))
        {
            sb.AppendLine($"{indent}Stack Trace:");
            foreach (var line in exception.StackTrace.Split('\n'))
            {
                sb.AppendLine($"{indent}  {line.Trim()}");
            }
        }

        if (exception.Data.Count > 0)
        {
            sb.AppendLine($"{indent}Data:");
            foreach (var key in exception.Data.Keys)
            {
                sb.AppendLine($"{indent}  {key} = {exception.Data[key]}");
            }
        }

        if (exception.InnerException != null)
        {
            sb.AppendLine();
            sb.AppendLine($"{indent}═══ INNER EXCEPTION ═══");
            AppendExceptionDetails(sb, exception.InnerException, depth + 1);
        }

        if (exception is AggregateException aggEx && aggEx.InnerExceptions.Count > 1)
        {
            for (var i = 1; i < aggEx.InnerExceptions.Count; i++)
            {
                sb.AppendLine();
                sb.AppendLine($"{indent}═══ AGGREGATE INNER EXCEPTION {i + 1} ═══");
                AppendExceptionDetails(sb, aggEx.InnerExceptions[i], depth + 1);
            }
        }
    }
}
