namespace TodoApp.Core.Models.Settings;

/// <summary>
/// Root settings object persisted to %AppData%/TodoApp/settings.json.
/// All sub-objects use sane defaults so a first-run is fully functional out of the box.
/// </summary>
public class AppSettings
{
    // ── Sub-settings ─────────────────────────────────────────────────────────
    public ThemeSettings        Theme         { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
    public SoundSettings        Sound         { get; set; } = new();
    public EmailSettings        Email         { get; set; } = new();
    public BackupSettings       Backup        { get; set; } = new();
    public CleanupSettings      Cleanup       { get; set; } = new();
    public FocusModeSettings    FocusMode     { get; set; } = new();
    public AISettings           AI            { get; set; } = new();

    // ── Startup / tray ───────────────────────────────────────────────────────
    public bool RunOnStartup       { get; set; } = true;
    public bool MinimizeToTray     { get; set; } = true;
    public bool ShowInTaskbar      { get; set; } = true;
    public bool StartMinimized     { get; set; } = false;

    // ── Global hotkey ────────────────────────────────────────────────────────
    /// <summary>"Ctrl+Alt+T" — parsed by NHotkey at startup.</summary>
    public string GlobalHotkey     { get; set; } = "Ctrl+Alt+T";

    // ── Default view ─────────────────────────────────────────────────────────
    /// <summary>"List" or "Kanban"</summary>
    public string DefaultView      { get; set; } = "List";

    // ── Data display ─────────────────────────────────────────────────────────
    /// <summary>Show only tasks created/due within this many days in default view.</summary>
    public int RecentDaysToShow    { get; set; } = 3;

    // ── Window state (persisted for next launch) ──────────────────────────────
    public double MainWindowLeft      { get; set; } = 100;
    public double MainWindowTop       { get; set; } = 100;
    public double MainWindowWidth     { get; set; } = 1200;
    public double MainWindowHeight    { get; set; } = 750;
    public bool   MainWindowMaximized { get; set; } = false;

    // ── Language ──────────────────────────────────────────────────────────────
    /// <summary>"vi" (default) or "en"</summary>
    public string Language { get; set; } = "vi";

    // ── Statistics ────────────────────────────────────────────────────────────
    /// <summary>Longest "completed every day" streak ever recorded.</summary>
    public int AllTimeStreak { get; set; } = 0;
}
