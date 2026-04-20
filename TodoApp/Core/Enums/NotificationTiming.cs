namespace TodoApp.Core.Enums;

/// <summary>
/// Bit-flag enum controlling which notification windows fire for a task.
/// Multiple timings can be combined: OneDayBefore | OneHourBefore | AtDeadline.
/// </summary>
[Flags]
public enum NotificationTiming
{
    None               = 0,
    OneDayBefore       = 1 << 0,   // 24 h before deadline
    OneHourBefore      = 1 << 1,   //  1 h before deadline
    FiveMinutesBefore  = 1 << 2,   //  5 m before deadline
    AtDeadline         = 1 << 3,   //  exactly at deadline
    OneDayAfter        = 1 << 4    // 24 h after deadline (overdue follow-up)
}
