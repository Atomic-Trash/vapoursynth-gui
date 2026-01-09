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

        // Setup global exception handlers
        SetupExceptionHandling();

        // Configure services BEFORE base.OnStartup() which creates the MainWindow
        // This ensures App.Services is available when ViewModels are instantiated
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        var logger = LoggingService.GetLogger<App>();
        logger.LogInformation("Application started");

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

        // Don't crash for non-fatal exceptions
        e.Handled = true;

        MessageBox.Show(
            $"An error occurred: {e.Exception.Message}\n\nCheck logs for details.",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var logger = LoggingService.GetLogger<App>();
        if (e.ExceptionObject is Exception ex)
        {
            logger.LogCritical(ex, "Unhandled exception (IsTerminating: {IsTerminating})", e.IsTerminating);
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
        services.AddSingleton<ThumbnailService>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<ProjectService>();
        services.AddSingleton<UndoService>();

        // ViewModels - transient (new instance per request)
        services.AddTransient<MediaViewModel>();
        services.AddTransient<EditViewModel>();
        services.AddTransient<ColorViewModel>();
        services.AddTransient<RestoreViewModel>();
        services.AddTransient<ExportViewModel>();
        services.AddTransient<MainViewModel>();
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
