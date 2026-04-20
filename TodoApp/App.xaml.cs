using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using TodoApp.Data;
using TodoApp.Infrastructure;
using TodoApp.Services;
using TodoApp.ViewModels;
using TodoApp.Views;
using NotificationService = TodoApp.Services.NotificationService;

namespace TodoApp;

public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        Program.Log("OnStartup() called.");
        base.OnStartup(e);

        DispatcherUnhandledException     += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException      += OnUnobservedTaskException;

        try
        {
            Program.Log("EnsureDirectoriesExist...");
            DataPaths.EnsureDirectoriesExist();

            Program.Log("Building IHost...");
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(RegisterServices)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddDebug();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .Build();
            Program.Log("IHost built OK.");

            ServiceLocator.Initialize(_host.Services);
            Program.Log("ServiceLocator initialized.");

            // Apply persisted language + theme before the window paints
            Program.Log("Applying language and theme...");
            var store = _host.Services.GetRequiredService<IAppDataStore>();
            var settings = store.GetSettingsAsync().GetAwaiter().GetResult();
            _host.Services.GetRequiredService<LocalizationService>().Apply(settings.Language);
            _host.Services.GetRequiredService<ThemeService>().Apply(settings.Theme);

            Program.Log("Resolving MainWindow...");
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            Program.Log("MainWindow resolved. Calling Show()...");
            mainWindow.Show();
            Program.Log("MainWindow.Show() called.");

            _ = _host.StartAsync();
            Program.Log("Host.StartAsync() fired.");
        }
        catch (Exception ex)
        {
            Program.Log($"EXCEPTION in OnStartup: {ex}");
            MessageBox.Show(
                $"Lỗi khởi động:\n\n{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}",
                "TodoApp — Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static void RegisterServices(IServiceCollection services)
    {
        Program.Log("RegisterServices() called.");

        // HTTP + AI
        services.AddHttpClient("Gemini", c =>
        {
            c.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddSingleton<IAIService, AIService>();

        // Data layer
        services.AddSingleton<IAppDataStore, AppDataStore>();

        // Services
        services.AddSingleton<ITodoService,          TodoService>();
        services.AddSingleton<IStatisticsService,    StatisticsService>();
        services.AddSingleton<ISoundService,         SoundService>();
        services.AddSingleton<INotificationService,  NotificationService>();
        services.AddHostedService(sp => (NotificationService)sp.GetRequiredService<INotificationService>());
        services.AddHostedService<RecurringTaskService>();
        services.AddSingleton<IStickyNoteService,    StickyNoteService>();
        services.AddSingleton<IWatchLaterService,    WatchLaterService>();
        services.AddSingleton<ILabelService,         LabelService>();
        services.AddSingleton<IGoalService,          GoalService>();
        services.AddSingleton<IEmailService,         EmailService>();
        services.AddSingleton<IBackupService,        BackupService>();
        services.AddHostedService<AutoBackupService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<LocalizationService>();

        // Infrastructure
        services.AddSingleton<SystemTrayService>();
        services.AddSingleton<GlobalHotkeyService>();
        services.AddSingleton<ToastService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<TaskListViewModel>();
        services.AddSingleton<KanbanViewModel>();
        services.AddSingleton<StatisticsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<StickyNotesViewModel>();
        services.AddSingleton<CalendarViewModel>();
        services.AddSingleton<QuickNoteViewModel>();
        services.AddSingleton<WatchLaterViewModel>();
        services.AddSingleton<LabelViewModel>();
        services.AddSingleton<GoalViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Program.Log("OnExit() called.");
        try
        {
            _host?.StopAsync(TimeSpan.FromMilliseconds(500)).GetAwaiter().GetResult();
            _host?.Dispose();
        }
        catch { }
        base.OnExit(e);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Program.Log($"DispatcherUnhandledException: {e.Exception.GetType().Name}: {e.Exception.Message}");
        // Only show MessageBox for truly unhandled (non-resource) errors
        if (e.Exception is not System.Windows.ResourceReferenceKeyNotFoundException)
        {
            MessageBox.Show(
                $"Lỗi runtime:\n\n{e.Exception.GetType().Name}: {e.Exception.Message}\n\n{e.Exception.StackTrace}",
                "TodoApp — Runtime Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        e.Handled = true;
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Program.Log($"DomainUnhandledException: {ex}");
            MessageBox.Show($"Lỗi nghiêm trọng:\n\n{ex.Message}", "TodoApp — Fatal",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Program.Log($"UnobservedTaskException: {e.Exception}");
        e.SetObserved();
    }
}
