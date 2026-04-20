using TodoApp.Core.Enums;

namespace TodoApp.Core.Models.Settings;

public class NotificationSettings
{
    public bool Enabled { get; set; } = true;

    /// <summary>Which timings are ON by default for newly created tasks.</summary>
    public NotificationTiming DefaultTimings { get; set; } =
        NotificationTiming.OneDayBefore   |
        NotificationTiming.OneHourBefore  |
        NotificationTiming.FiveMinutesBefore |
        NotificationTiming.AtDeadline;

    // ── Snooze ───────────────────────────────────────────────────────────────
    /// <summary>Default snooze length in minutes when user clicks "Snooze".</summary>
    public int   DefaultSnoozeDuration { get; set; } = 15;
    public int[] SnoozeOptions         { get; set; } = { 5, 10, 15, 30, 60 };

    // ── Toast display ────────────────────────────────────────────────────────
    public int  ToastDisplaySeconds { get; set; } = 8;

    // ── Grouping ─────────────────────────────────────────────────────────────
    /// <summary>Bundle notifications when multiple tasks are due within a short window.</summary>
    public bool GroupNotifications      { get; set; } = true;
    public int  GroupThresholdMinutes   { get; set; } = 10;

    // ── Background polling ───────────────────────────────────────────────────
    /// <summary>How often (seconds) the background service checks for due notifications.</summary>
    public int CheckIntervalSeconds { get; set; } = 30;

    // ── Startup recovery ─────────────────────────────────────────────────────
    public bool DetectMissedOnStartup { get; set; } = true;
}
