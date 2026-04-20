using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TodoApp.Core.Models.Settings;
using TodoApp.Data;
using TodoApp.Services;
using TodoApp.ViewModels.Base;

namespace TodoApp.ViewModels;

/// <summary>
/// Shell ViewModel — owns application-wide state: navigation, settings, focus mode.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly IAppDataStore          _dataStore;
    private readonly ILogger<MainViewModel> _logger;
    private readonly IServiceProvider       _services;

    // Focus-mode countdown
    private readonly DispatcherTimer _focusTimer;
    private int                      _focusSecondsLeft;

    // Auto dark mode – checks every minute
    private readonly DispatcherTimer _autoThemeTimer;

    // ── Observable state ─────────────────────────────────────────────────────

    [ObservableProperty]
    private AppSettings _settings = new();

    /// <summary>Current page ViewModel shown in the ContentControl.</summary>
    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    [ObservableProperty]
    private string _currentPage = "List";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isFocusModeActive;

    /// <summary>Formatted remaining focus time, e.g. "24:38".</summary>
    [ObservableProperty]
    private string _remainingFocusTime = "25:00";

    [ObservableProperty]
    private int _remainingFocusMinutes;

    [ObservableProperty]
    private int _pendingNotificationCount;

    // ── Constructor ───────────────────────────────────────────────────────────

    public MainViewModel(
        IAppDataStore dataStore,
        ILogger<MainViewModel> logger,
        IServiceProvider services)
    {
        _dataStore = dataStore;
        _logger    = logger;
        _services  = services;

        _focusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _focusTimer.Tick += OnFocusTimerTick;

        _autoThemeTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _autoThemeTimer.Tick += (_, _) => CheckAndApplyAutoTheme();
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    public override async Task InitializeAsync()
    {
        await RunBusyAsync(async () =>
        {
            Settings = await _dataStore.GetSettingsAsync();
            IsFocusModeActive = Settings.FocusMode.IsActive;
            _logger.LogInformation("MainViewModel initialised. DefaultView={View}", Settings.DefaultView);
        }, "Loading settings…");

        // Navigate to default page after settings load
        await NavigateToAsync(Settings.DefaultView ?? "List");

        // Start auto-theme timer and check immediately
        CheckAndApplyAutoTheme();
        _autoThemeTimer.Start();
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private async Task NavigateToAsync(string page)
    {
        if (CurrentPage == page && CurrentViewModel != null) return;

        CurrentPage = page;

        ViewModelBase? vm = page switch
        {
            "List"        => _services.GetRequiredService<TaskListViewModel>(),
            "Kanban"      => _services.GetRequiredService<KanbanViewModel>(),
            "Statistics"  => _services.GetRequiredService<StatisticsViewModel>(),
            "Settings"    => _services.GetRequiredService<SettingsViewModel>(),
            "StickyNotes" => _services.GetRequiredService<StickyNotesViewModel>(),
            "Calendar"    => _services.GetRequiredService<CalendarViewModel>(),
            "WatchLater"  => _services.GetRequiredService<WatchLaterViewModel>(),
            "Labels"      => _services.GetRequiredService<LabelViewModel>(),
            "Goals"       => _services.GetRequiredService<GoalViewModel>(),
            _             => null
        };

        if (vm != null && vm != CurrentViewModel)
            await vm.InitializeAsync();

        CurrentViewModel = vm;
    }

    [RelayCommand]
    private void NavigateTo(string page) => _ = NavigateToAsync(page);

    // ── Focus mode ────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ToggleFocusModeAsync()
    {
        IsFocusModeActive           = !IsFocusModeActive;
        Settings.FocusMode.IsActive = IsFocusModeActive;
        await _dataStore.SaveSettingsAsync(Settings);

        if (IsFocusModeActive)
        {
            var durationMinutes = Settings.FocusMode.DefaultDurationMinutes > 0
                ? Settings.FocusMode.DefaultDurationMinutes : 25;
            _focusSecondsLeft = durationMinutes * 60;
            UpdateFocusDisplay();
            _focusTimer.Start();
            StatusMessage = "Focus mode ON";
            _logger.LogInformation("Focus mode started: {Min} min.", durationMinutes);
        }
        else
        {
            _focusTimer.Stop();
            RemainingFocusTime    = FormatFocusTime(Settings.FocusMode.DefaultDurationMinutes > 0
                ? Settings.FocusMode.DefaultDurationMinutes : 25, 0);
            RemainingFocusMinutes = Settings.FocusMode.DefaultDurationMinutes > 0
                ? Settings.FocusMode.DefaultDurationMinutes : 25;
            StatusMessage = "Focus mode OFF";
        }
    }

    private void OnFocusTimerTick(object? sender, EventArgs e)
    {
        if (_focusSecondsLeft <= 0)
        {
            _focusTimer.Stop();
            IsFocusModeActive           = false;
            Settings.FocusMode.IsActive = false;
            _ = _dataStore.SaveSettingsAsync(Settings);
            StatusMessage = "Focus session complete!";
            _logger.LogInformation("Focus session ended.");
            return;
        }

        _focusSecondsLeft--;
        UpdateFocusDisplay();
    }

    private void UpdateFocusDisplay()
    {
        int mins = _focusSecondsLeft / 60;
        int secs = _focusSecondsLeft % 60;
        RemainingFocusTime    = $"{mins:D2}:{secs:D2}";
        RemainingFocusMinutes = mins;
    }

    private static string FormatFocusTime(int minutes, int seconds)
        => $"{minutes:D2}:{seconds:D2}";

    // ── Auto Dark Mode ────────────────────────────────────────────────────────

    private void CheckAndApplyAutoTheme()
    {
        var theme = Settings.Theme;
        if (!theme.AutoDarkMode) return;

        var hour        = DateTime.Now.Hour;
        var shouldDark  = IsDarkHour(hour, theme.DarkModeStartHour, theme.LightModeStartHour);
        var newMode     = shouldDark ? "Dark" : "Light";
        if (theme.Mode == newMode) return;

        theme.Mode = newMode;
        _services.GetRequiredService<ThemeService>().Apply(theme);
        _ = _dataStore.SaveSettingsAsync(Settings);
        _logger.LogInformation("Auto dark mode: switched to {Mode} at hour {Hour}", newMode, hour);
    }

    /// <summary>Returns true when <paramref name="hour"/> falls inside the dark window.</summary>
    private static bool IsDarkHour(int hour, int darkStart, int lightStart)
        => darkStart >= lightStart
            ? hour >= darkStart || hour < lightStart   // wraps midnight, e.g. 18→06
            : hour >= darkStart && hour < lightStart;  // dark is daytime (unusual)

    // ── Settings persistence ──────────────────────────────────────────────────

    public async Task SaveSettingsAsync()
    {
        try   { await _dataStore.SaveSettingsAsync(Settings); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings.");
            StatusMessage = "Failed to save settings.";
        }
    }
}
