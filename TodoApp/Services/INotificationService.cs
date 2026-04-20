using TodoApp.Core.Enums;
using TodoApp.Core.Models;

namespace TodoApp.Services;

/// <summary>
/// Raised when a task notification becomes due.
/// </summary>
public sealed class NotificationEventArgs : EventArgs
{
    public TodoItem        Task    { get; init; } = null!;
    public NotificationTiming Timing { get; init; }
    public bool            IsSnoozeWakeup { get; init; }
}

public interface INotificationService
{
    /// <summary>Fired on the UI dispatcher when a notification window should appear.</summary>
    event EventHandler<NotificationEventArgs> NotificationTriggered;

    /// <summary>Snooze a specific notification for <paramref name="duration"/>.</summary>
    Task SnoozeAsync(Guid taskId, NotificationTiming timing, TimeSpan duration);

    /// <summary>Permanently dismiss a notification (marks IsSent=true without showing again).</summary>
    Task DismissAsync(Guid taskId, NotificationTiming timing);

    /// <summary>Number of unread/pending notification badges for the taskbar.</summary>
    int PendingCount { get; }
}
