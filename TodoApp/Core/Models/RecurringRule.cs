using TodoApp.Core.Enums;

namespace TodoApp.Core.Models;

/// <summary>
/// Defines how and when a recurring task template should generate new instances.
/// Stored on the template TodoItem (IsRecurring = true).
/// </summary>
public class RecurringRule
{
    public RecurrenceType Type { get; set; } = RecurrenceType.Daily;

    /// <summary>Weekly recurrence: which days of the week. Empty = every day.</summary>
    public List<DayOfWeek> WeekDays { get; set; } = new();

    /// <summary>Monthly recurrence: day of month (1–31). -1 = last day.</summary>
    public int DayOfMonth { get; set; } = 1;

    /// <summary>Custom recurrence: repeat every N days.</summary>
    public int IntervalDays { get; set; } = 1;

    /// <summary>Stop generating after this date (null = forever).</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Stop after this many generated instances (0 = unlimited).</summary>
    public int MaxInstances { get; set; } = 0;

    /// <summary>Number of instances generated so far (maintained by RecurringTaskService).</summary>
    public int GeneratedCount { get; set; } = 0;

    /// <summary>Timestamp of the last generated instance's deadline, for "next run" calculation.</summary>
    public DateTime? LastGeneratedAt { get; set; }
}
