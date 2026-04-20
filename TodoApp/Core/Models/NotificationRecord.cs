using TodoApp.Core.Enums;

namespace TodoApp.Core.Models;

/// <summary>
/// Tracks every notification event for a task.
/// Prevents duplicates and enables missed-notification detection on app restart.
/// </summary>
public class NotificationRecord
{
    public Guid Id              { get; set; } = Guid.NewGuid();
    public NotificationTiming Timing { get; set; }

    /// <summary>When this notification was supposed to fire.</summary>
    public DateTime ScheduledAt  { get; set; }

    /// <summary>Null until the toast is actually shown.</summary>
    public DateTime? SentAt      { get; set; }
    public bool IsSent           => SentAt.HasValue;

    /// <summary>True when app restarted and found this notification was missed.</summary>
    public bool WasMissed        { get; set; }

    /// <summary>If user snoozed: when it should re-fire.</summary>
    public DateTime? SnoozedUntil { get; set; }
    public bool IsSnoozed        => SnoozedUntil.HasValue && SnoozedUntil.Value > DateTime.Now;

    /// <summary>Email counterpart was dispatched.</summary>
    public bool EmailSent        { get; set; }
}
