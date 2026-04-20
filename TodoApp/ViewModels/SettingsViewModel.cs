using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TodoApp.Core.Models.Settings;
using TodoApp.Data;
using TodoApp.Services;
using TodoApp.ViewModels.Base;

namespace TodoApp.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IAppDataStore           _store;
    private readonly IBackupService          _backup;
    private readonly IEmailService           _email;
    private readonly ThemeService            _theme;
    private readonly LocalizationService     _loc;
    private readonly IAIService              _ai;
    private readonly ISoundService           _sound;
    private readonly ILogger<SettingsViewModel> _logger;

    // ── Active tab ────────────────────────────────────────────────────────────
    [ObservableProperty] private string _activeTab = "General";

    // ── Status message ────────────────────────────────────────────────────────
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool   _isSuccess;

    // ═══════════════════════════════════════════════════════════════════════
    //  General
    // ═══════════════════════════════════════════════════════════════════════
    [ObservableProperty] private bool   _runOnStartup    = true;
    [ObservableProperty] private bool   _minimizeToTray  = true;
    [ObservableProperty] private bool   _startMinimized  = false;
    [ObservableProperty] private bool   _showInTaskbar   = true;
    [ObservableProperty] private string _defaultView     = "List";
    [ObservableProperty] private string _globalHotkey    = "Ctrl+Alt+T";

    // ═══════════════════════════════════════════════════════════════════════
    //  Appearance
    // ═══════════════════════════════════════════════════════════════════════
    [ObservableProperty] private string _themeMode        = "Light";
    [ObservableProperty] private string _themePreset      = "Pink";
    [ObservableProperty] private bool   _useAnimations    = true;
    [ObservableProperty] private bool   _showConfetti     = true;
    [ObservableProperty] private bool   _autoDarkMode     = false;
    [ObservableProperty] private int    _darkModeStartHour  = 18;
    [ObservableProperty] private int    _lightModeStartHour = 6;

    // Colour overrides
    [ObservableProperty] private string _primaryColor     = "#E91E63";
    [ObservableProperty] private string _bgColor          = "#FFF5F9";

    // ═══════════════════════════════════════════════════════════════════════
    //  Notifications
    // ═══════════════════════════════════════════════════════════════════════
    [ObservableProperty] private bool _notificationsEnabled    = true;
    [ObservableProperty] private bool _notifyOneDayBefore      = true;
    [ObservableProperty] private bool _notifyOneHourBefore     = true;
    [ObservableProperty] private bool _notifyFiveMinBefore     = true;
    [ObservableProperty] private bool _notifyAtDeadline        = true;
    [ObservableProperty] private bool _notifyOneDayAfter;
    [ObservableProperty] private int  _toastDisplaySeconds     = 8;
    [ObservableProperty] private int  _checkIntervalSeconds    = 30;
    [ObservableProperty] private bool _detectMissedOnStartup   = true;

    // ═══════════════════════════════════════════════════════════════════════
    //  Sound
    // ═══════════════════════════════════════════════════════════════════════
    [ObservableProperty] private bool   _soundEnabled          = true;
    [ObservableProperty] private double _soundVolume           = 0.7;
    [ObservableProperty] private bool   _playCompletionSound   = true;
    [ObservableProperty] private string _completionSoundPath   = "";

    // ═══════════════════════════════════════════════════════════════════════
    //  Email
    // ═══════════════════════════════════════════════════════════════════════
    [ObservableProperty] private bool   _emailEnabled          = false;
    [ObservableProperty] private string _emailProvider         = "Brevo";
    [ObservableProperty] private string _brevoApiKey           = "";
    [ObservableProperty] private string _senderEmail           = "";
    [ObservableProperty] private string _senderName            = "TodoApp";
    [ObservableProperty] private string _recipientEmail        = "";
    [ObservableProperty] private string _recipientName         = "";
    [ObservableProperty] private bool   _emailOneDayBefore     = true;
    [ObservableProperty] private bool   _emailOneHourBefore    = true;
    [ObservableProperty] private bool   _emailFiveMinBefore    = false;

    // SMTP fallback
    [ObservableProperty] private string _smtpHost     = "";
    [ObservableProperty] private int    _smtpPort     = 587;
    [ObservableProperty] private string _smtpUser     = "";
    [ObservableProperty] private string _smtpPassword = "";
    [ObservableProperty] private bool   _smtpUseSsl   = true;

    // ═══════════════════════════════════════════════════════════════════════
    //  Backup
    // ═══════════════════════════════════════════════════════════════════════
    [ObservableProperty] private bool   _autoBackupEnabled        = true;
    [ObservableProperty] private int    _autoBackupIntervalHours  = 24;
    [ObservableProperty] private int    _maxBackupFiles           = 10;
    [ObservableProperty] private string _backupDirectory          = "";
    [ObservableProperty] private bool   _backupOnExit             = false;
    [ObservableProperty] private string _lastBackupText           = "No backup yet";

    // ═══════════════════════════════════════════════════════════════════════
    //  Focus Mode
    // ═══════════════════════════════════════════════════════════════════════
    [ObservableProperty] private int  _focusDurationMinutes       = 25;
    [ObservableProperty] private bool _focusSuppressToasts        = true;
    [ObservableProperty] private bool _focusSuppressSounds        = false;
    [ObservableProperty] private bool _focusPlayEndSound          = true;

    // ═══════════════════════════════════════════════════════════════════════
    //  Language
    // ═══════════════════════════════════════════════════════════════════════
    [ObservableProperty] private string _language = "vi";

    // ═══════════════════════════════════════════════════════════════════════
    //  AI Assistant
    // ═══════════════════════════════════════════════════════════════════════
    [ObservableProperty] private bool   _aiEnabled      = false;
    [ObservableProperty] private string _geminiApiKey   = "";
    [ObservableProperty] private string _geminiModel    = "gemini-2.0-flash";
    [ObservableProperty] private string _aiTestResult   = "";
    [ObservableProperty] private bool   _aiTestSuccess  = false;

    // ── Constructor ───────────────────────────────────────────────────────────

    public SettingsViewModel(
        IAppDataStore store,
        IBackupService backup,
        IEmailService  email,
        ThemeService   theme,
        LocalizationService loc,
        IAIService     ai,
        ISoundService  sound,
        ILogger<SettingsViewModel> logger)
    {
        _store  = store;
        _backup = backup;
        _email  = email;
        _theme  = theme;
        _loc    = loc;
        _ai     = ai;
        _sound  = sound;
        _logger = logger;
    }

    // ── Load / Save ───────────────────────────────────────────────────────────

    public override async Task InitializeAsync()
        => await RunBusyAsync(LoadAsync, "Loading settings…");

    private async Task LoadAsync()
    {
        var s = await _store.GetSettingsAsync();
        LoadFrom(s);
        ApplyStartupRegistry(s.RunOnStartup);
    }

    private void LoadFrom(AppSettings s)
    {
        RunOnStartup   = s.RunOnStartup;
        MinimizeToTray = s.MinimizeToTray;
        StartMinimized = s.StartMinimized;
        ShowInTaskbar  = s.ShowInTaskbar;
        DefaultView    = s.DefaultView;
        GlobalHotkey   = s.GlobalHotkey;

        ThemeMode          = s.Theme.Mode;
        ThemePreset        = s.Theme.Preset;
        UseAnimations      = s.Theme.UseAnimations;
        ShowConfetti       = s.Theme.ShowConfetti;
        AutoDarkMode       = s.Theme.AutoDarkMode;
        DarkModeStartHour  = s.Theme.DarkModeStartHour;
        LightModeStartHour = s.Theme.LightModeStartHour;
        PrimaryColor       = s.Theme.PrimaryColor;
        BgColor            = s.Theme.BackgroundColor;

        NotificationsEnabled   = s.Notifications.Enabled;
        NotifyOneDayBefore     = s.Notifications.DefaultTimings.HasFlag(Core.Enums.NotificationTiming.OneDayBefore);
        NotifyOneHourBefore    = s.Notifications.DefaultTimings.HasFlag(Core.Enums.NotificationTiming.OneHourBefore);
        NotifyFiveMinBefore    = s.Notifications.DefaultTimings.HasFlag(Core.Enums.NotificationTiming.FiveMinutesBefore);
        NotifyAtDeadline       = s.Notifications.DefaultTimings.HasFlag(Core.Enums.NotificationTiming.AtDeadline);
        NotifyOneDayAfter      = s.Notifications.DefaultTimings.HasFlag(Core.Enums.NotificationTiming.OneDayAfter);
        ToastDisplaySeconds    = s.Notifications.ToastDisplaySeconds;
        CheckIntervalSeconds   = s.Notifications.CheckIntervalSeconds;
        DetectMissedOnStartup  = s.Notifications.DetectMissedOnStartup;

        SoundEnabled        = s.Sound.Enabled;
        SoundVolume         = s.Sound.Volume;
        PlayCompletionSound = s.Sound.PlayCompletionSound;
        CompletionSoundPath = s.Sound.CompletionSoundPath ?? "";

        EmailEnabled       = s.Email.Enabled;
        EmailProvider      = s.Email.Provider;
        BrevoApiKey        = s.Email.BrevoApiKey;
        SenderEmail        = s.Email.SenderEmail;
        SenderName         = s.Email.SenderName;
        RecipientEmail     = s.Email.RecipientEmail;
        RecipientName      = s.Email.RecipientName;
        EmailOneDayBefore  = s.Email.SendOneDayBefore;
        EmailOneHourBefore = s.Email.SendOneHourBefore;
        EmailFiveMinBefore = s.Email.SendFiveMinutesBefore;
        SmtpHost           = s.Email.SmtpHost;
        SmtpPort           = s.Email.SmtpPort;
        SmtpUser           = s.Email.SmtpUser;
        SmtpPassword       = s.Email.SmtpPassword;
        SmtpUseSsl         = s.Email.SmtpUseSsl;

        AutoBackupEnabled       = s.Backup.AutoBackupEnabled;
        AutoBackupIntervalHours = s.Backup.AutoBackupIntervalHours;
        MaxBackupFiles          = s.Backup.MaxBackupFiles;
        BackupDirectory         = s.Backup.BackupDirectory;
        BackupOnExit            = s.Backup.BackupOnExit;

        FocusDurationMinutes = s.FocusMode.DefaultDurationMinutes;
        FocusSuppressToasts  = s.FocusMode.SuppressToastNotifications;
        FocusSuppressSounds  = s.FocusMode.SuppressSounds;
        FocusPlayEndSound    = s.FocusMode.PlayEndSound;

        Language = s.Language;

        AiEnabled    = s.AI.Enabled;
        GeminiApiKey = s.AI.GeminiApiKey;
        GeminiModel  = s.AI.ModelName;
    }

    private AppSettings BuildSettings(AppSettings existing)
    {
        existing.RunOnStartup   = RunOnStartup;
        existing.MinimizeToTray = MinimizeToTray;
        existing.StartMinimized = StartMinimized;
        existing.ShowInTaskbar  = ShowInTaskbar;
        existing.DefaultView    = DefaultView;
        existing.GlobalHotkey   = GlobalHotkey;

        existing.Theme.Mode             = ThemeMode;
        existing.Theme.Preset           = ThemePreset;
        existing.Theme.UseAnimations    = UseAnimations;
        existing.Theme.ShowConfetti     = ShowConfetti;
        existing.Theme.AutoDarkMode     = AutoDarkMode;
        existing.Theme.DarkModeStartHour  = DarkModeStartHour;
        existing.Theme.LightModeStartHour = LightModeStartHour;
        existing.Theme.PrimaryColor     = PrimaryColor;
        existing.Theme.BackgroundColor  = BgColor;

        existing.Notifications.Enabled              = NotificationsEnabled;
        existing.Notifications.ToastDisplaySeconds  = ToastDisplaySeconds;
        existing.Notifications.CheckIntervalSeconds = CheckIntervalSeconds;
        existing.Notifications.DetectMissedOnStartup = DetectMissedOnStartup;

        var timings = Core.Enums.NotificationTiming.None;
        if (NotifyOneDayBefore)  timings |= Core.Enums.NotificationTiming.OneDayBefore;
        if (NotifyOneHourBefore) timings |= Core.Enums.NotificationTiming.OneHourBefore;
        if (NotifyFiveMinBefore) timings |= Core.Enums.NotificationTiming.FiveMinutesBefore;
        if (NotifyAtDeadline)    timings |= Core.Enums.NotificationTiming.AtDeadline;
        if (NotifyOneDayAfter)   timings |= Core.Enums.NotificationTiming.OneDayAfter;
        existing.Notifications.DefaultTimings = timings;

        existing.Sound.Enabled             = SoundEnabled;
        existing.Sound.Volume              = (float)SoundVolume;
        existing.Sound.PlayCompletionSound = PlayCompletionSound;
        existing.Sound.CompletionSoundPath = string.IsNullOrWhiteSpace(CompletionSoundPath)
            ? null : CompletionSoundPath;

        existing.Email.Enabled                 = EmailEnabled;
        existing.Email.Provider                = EmailProvider;
        existing.Email.BrevoApiKey             = BrevoApiKey;
        existing.Email.SenderEmail             = SenderEmail;
        existing.Email.SenderName              = SenderName;
        existing.Email.RecipientEmail          = RecipientEmail;
        existing.Email.RecipientName           = RecipientName;
        existing.Email.SendOneDayBefore        = EmailOneDayBefore;
        existing.Email.SendOneHourBefore       = EmailOneHourBefore;
        existing.Email.SendFiveMinutesBefore   = EmailFiveMinBefore;
        existing.Email.SmtpHost                = SmtpHost;
        existing.Email.SmtpPort                = SmtpPort;
        existing.Email.SmtpUser                = SmtpUser;
        existing.Email.SmtpPassword            = SmtpPassword;
        existing.Email.SmtpUseSsl              = SmtpUseSsl;

        existing.Backup.AutoBackupEnabled      = AutoBackupEnabled;
        existing.Backup.AutoBackupIntervalHours = AutoBackupIntervalHours;
        existing.Backup.MaxBackupFiles         = MaxBackupFiles;
        existing.Backup.BackupDirectory        = BackupDirectory;
        existing.Backup.BackupOnExit           = BackupOnExit;

        existing.FocusMode.DefaultDurationMinutes     = FocusDurationMinutes;
        existing.FocusMode.SuppressToastNotifications = FocusSuppressToasts;
        existing.FocusMode.SuppressSounds             = FocusSuppressSounds;
        existing.FocusMode.PlayEndSound               = FocusPlayEndSound;

        existing.Language = Language;

        existing.AI.Enabled      = AiEnabled;
        existing.AI.GeminiApiKey = GeminiApiKey;
        existing.AI.ModelName    = GeminiModel;

        return existing;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void SwitchTab(string tab) => ActiveTab = tab;

    /// <summary>Apply a named colour preset as a live preview (doesn't save).</summary>
    [RelayCommand]
    private void ApplyPreset(string preset)
    {
        ThemePreset = preset;
        _theme.Apply(new ThemeSettings
        {
            Mode   = ThemeMode,
            Preset = preset
        });
    }

    /// <summary>Toggle Light/Dark mode live preview.</summary>
    [RelayCommand]
    private void ApplyThemeMode(string mode)
    {
        ThemeMode = mode;
        _theme.Apply(new ThemeSettings
        {
            Mode   = mode,
            Preset = ThemePreset,
            PrimaryColor = PrimaryColor,
            BackgroundColor = BgColor
        });
    }

    /// <summary>Switch language immediately (live — no restart required).</summary>
    [RelayCommand]
    private void ChangeLanguage(string lang)
    {
        Language = lang;
        _loc.Apply(lang);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await RunBusyAsync(async () =>
        {
            var current = await _store.GetSettingsAsync();
            BuildSettings(current);
            await _store.SaveSettingsAsync(current);

            // Apply theme immediately — no restart needed
            _theme.Apply(current.Theme);

            ApplyStartupRegistry(current.RunOnStartup);

            ShowStatus(_loc.Translate("Msg_SettingsSaved"), success: true);
            _logger.LogInformation("Settings saved by user.");
        }, "Saving…");
    }

    private static void ApplyStartupRegistry(bool enable)
    {
        const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        const string valueName = "TodoApp";
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
            if (key is null) return;
            if (enable)
            {
                var exePath = Environment.ProcessPath
                    ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                    key.SetValue(valueName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(valueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Startup] Registry error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task TestEmailAsync()
    {
        await RunBusyAsync(async () =>
        {
            var settings = BuildSettings(await _store.GetSettingsAsync());
            var result   = await _email.SendTestEmailAsync(settings.Email);
            ShowStatus(result ? "Test email sent!" : "Failed to send email. Check your settings.", result);
        }, "Sending test email…");
    }

    [RelayCommand]
    private async Task BackupNowAsync()
    {
        await RunBusyAsync(async () =>
        {
            var path = await _backup.CreateBackupAsync();
            LastBackupText = $"Last backup: {DateTime.Now:g}";
            ShowStatus($"Backup created: {System.IO.Path.GetFileName(path)}", success: true);
        }, "Creating backup…");
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "TodoApp Backup (*.zip)|*.zip",
            Title  = "Select backup to restore"
        };
        if (dlg.ShowDialog() != true) return;

        var confirm = MessageBox.Show(
            "Restoring will replace all current tasks and settings. Continue?",
            "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        await RunBusyAsync(async () =>
        {
            await _backup.RestoreBackupAsync(dlg.FileName);
            await LoadAsync();
            ShowStatus("Restore complete. Restart recommended.", success: true);
        }, "Restoring…");
    }

    [RelayCommand]
    private void BrowseBackupDir()
    {
        // Use OpenFileDialog as a folder picker (select any file; we take the directory).
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title            = "Select any file inside the desired backup folder",
            Filter           = "All files (*.*)|*.*",
            CheckFileExists  = false,
            FileName         = "Select folder",
            ValidateNames    = false,
        };
        if (BackupDirectory is { Length: > 0 } && System.IO.Directory.Exists(BackupDirectory))
            dlg.InitialDirectory = BackupDirectory;

        if (dlg.ShowDialog() == true)
            BackupDirectory = System.IO.Path.GetDirectoryName(dlg.FileName) ?? BackupDirectory;
    }

    [RelayCommand]
    private async Task ExportTasksAsync()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title      = _loc.Translate("Export_Title"),
            Filter     = "JSON file (*.json)|*.json",
            FileName   = $"todoapp_export_{DateTime.Now:yyyyMMdd_HHmmss}.json",
            DefaultExt = ".json"
        };
        if (dlg.ShowDialog() != true) return;

        await RunBusyAsync(async () =>
        {
            await _backup.ExportTasksAsync(dlg.FileName);
            ShowStatus(string.Format(_loc.Translate("Export_Success"),
                       System.IO.Path.GetFileName(dlg.FileName)), success: true);
        }, _loc.Translate("Export_Exporting"));
    }

    [RelayCommand]
    private async Task ImportTasksAsync()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title  = _loc.Translate("Import_Title"),
            Filter = "JSON file (*.json)|*.json"
        };
        if (dlg.ShowDialog() != true) return;

        // Ask merge vs replace
        var result = MessageBox.Show(
            _loc.Translate("Import_MergePrompt"),
            _loc.Translate("Import_Title"),
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel) return;
        bool merge = result == MessageBoxResult.Yes;

        await RunBusyAsync(async () =>
        {
            int count = await _backup.ImportTasksAsync(dlg.FileName, merge);
            ShowStatus(string.Format(_loc.Translate("Import_Success"), count), success: true);
            // Invalidate cache so new tasks appear immediately
            _store.InvalidateCache();
        }, _loc.Translate("Import_Importing"));
    }

    [RelayCommand]
    private async Task TestAiConnectionAsync()
    {
        AiTestResult  = "";
        AiTestSuccess = false;

        if (string.IsNullOrWhiteSpace(GeminiApiKey))
        {
            AiTestResult  = "Vui lòng nhập API Key trước.";
            AiTestSuccess = false;
            return;
        }

        // Save current key temporarily so AIService can read it
        var current = await _store.GetSettingsAsync();
        current.AI.Enabled      = true;
        current.AI.GeminiApiKey = GeminiApiKey;
        current.AI.ModelName    = GeminiModel;
        await _store.SaveSettingsAsync(current);

        await RunBusyAsync(async () =>
        {
            var result = await _ai.SuggestTagsAsync("Kiểm tra kết nối AI", "", default);
            if (result.Success)
            {
                AiTestResult  = $"✓ Kết nối thành công! Tag mẫu: {string.Join(", ", result.Tags)}";
                AiTestSuccess = true;
            }
            else
            {
                AiTestResult  = $"✗ Thất bại: {result.Error}";
                AiTestSuccess = false;
            }
        }, "Đang kiểm tra kết nối AI…");
    }

    [RelayCommand]
    private void BrowseCompletionSound()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Audio files (*.wav;*.mp3)|*.wav;*.mp3",
            Title  = "Select completion sound"
        };
        if (dlg.ShowDialog() == true)
            CompletionSoundPath = dlg.FileName;
    }

    [RelayCommand]
    private async Task TestCompletionSound()
    {
        if (!SoundEnabled) return;
        if (!string.IsNullOrWhiteSpace(CompletionSoundPath))
            await _sound.PlayFileAsync(CompletionSoundPath, (float)SoundVolume);
        else
            await _sound.PlayCompletionAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ShowStatus(string msg, bool success)
    {
        StatusMessage = msg;
        IsSuccess     = success;
    }
}
