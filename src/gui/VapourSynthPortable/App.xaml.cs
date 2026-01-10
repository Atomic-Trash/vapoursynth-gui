using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Initialize logging first
        LoggingService.Initialize();

        // Initialize crash reporter (after logging)
        CrashReporter.Initialize();

        // Setup global exception handlers
        SetupExceptionHandling();

        // Configure services BEFORE base.OnStartup() which creates the MainWindow
        // This ensures App.Services is available when ViewModels are instantiated
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        var logger = LoggingService.GetLogger<App>();
        logger.LogInformation("Application started");

        // Cleanup old crash reports (keep last 10)
        CrashReporter.CleanupOldReports(10);

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        var logger = LoggingService.GetLogger<App>();
        logger.LogInformation("Application exiting");

        LoggingService.Shutdown();
        base.OnExit(e);
    }

    private void SetupExceptionHandling()
    {
        // Handle UI thread exceptions
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // Handle non-UI thread exceptions
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // Handle task exceptions
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var logger = LoggingService.GetLogger<App>();
        logger.LogError(e.Exception, "Unhandled UI thread exception");

        // Create crash report
        var crashReportPath = CrashReporter.CreateCrashReport(e.Exception, "DispatcherUnhandled", isTerminating: false);

        // Don't crash for non-fatal exceptions
        e.Handled = true;

        // Show user-friendly dialog with crash report path
        CrashReporter.ShowCrashDialog(e.Exception, crashReportPath);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var logger = LoggingService.GetLogger<App>();
        if (e.ExceptionObject is Exception ex)
        {
            logger.LogCritical(ex, "Unhandled exception (IsTerminating: {IsTerminating})", e.IsTerminating);

            // Create crash report for fatal exceptions
            CrashReporter.CreateCrashReport(ex, "AppDomain.UnhandledException", e.IsTerminating);
        }
        else
        {
            logger.LogCritical("Unhandled non-exception error: {Error}", e.ExceptionObject);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        var logger = LoggingService.GetLogger<App>();
        logger.LogError(e.Exception, "Unobserved task exception");

        // Create crash report for task exceptions
        CrashReporter.CreateCrashReport(e.Exception, "TaskScheduler.UnobservedTaskException", isTerminating: false);

        // Prevent app crash
        e.SetObserved();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddSingleton(LoggingService.GetLoggerFactory());
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        // Core services - singletons shared across all pages
        services.AddSingleton<IMediaPoolService, MediaPoolService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IPluginService, PluginService>();
        services.AddSingleton<IBuildService, BuildService>();
        services.AddSingleton<IVapourSynthService, VapourSynthService>();
        services.AddSingleton<ThumbnailService>();
        services.AddSingleton<UndoService>();

        // ViewModels - transient (new instance per request)
        services.AddTransient<MediaViewModel>();
        services.AddTransient<EditViewModel>();
        services.AddTransient<ColorViewModel>();
        services.AddTransient<RestoreViewModel>();
        services.AddTransient<ExportViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<MainWindowViewModel>();
    }

    /// <summary>
    /// Get a service from the DI container
    /// </summary>
    public static T GetService<T>() where T : class
    {
        return Services.GetRequiredService<T>();
    }

    /// <summary>
    /// Try to get a service from the DI container
    /// </summary>
    public static T? TryGetService<T>() where T : class
    {
        return Services.GetService<T>();
    }
}
