using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TodoApp.Core.Enums;
using TodoApp.Core.Models;
using TodoApp.Services;

namespace TodoApp.ViewModels;

/// <summary>
/// Backing ViewModel for a single toast notification popup.
/// Fires <see cref="CloseRequested"/> when the user acts or the timer runs out.
/// </summary>
public partial class ToastViewModel : ObservableObject
{
    private readonly INotificationService _notifications;

    public event Action? CloseRequested;

    // ── Display data ──────────────────────────────────────────────────────────

    public Guid              TaskId    { get; }
    public NotificationTiming Timing   { get; }
    public string            TaskTitle { get; }
    public string            TimingLabel { get; }
    public string            DeadlineText { get; }
    public string            PriorityLabel { get; }
    public bool              IsOverdue { get; }

    // ── Auto-dismiss countdown ────────────────────────────────────────────────

    private const int TotalSeconds = 10;

    [ObservableProperty] private int    _secondsLeft = TotalSeconds;
    [ObservableProperty] private double _dismissProgress = 100;   // 100 → 0

    public ToastViewModel(
        TodoItem task,
        NotificationTiming timing,
        INotificationService notifications)
    {
        _notifications = notifications;
        TaskId         = task.Id;
        Timing         = timing;
        TaskTitle      = task.Title;
        PriorityLabel  = task.PriorityLabel;
        IsOverdue      = task.IsOverdue;

        TimingLabel = timing switch
        {
            NotificationTiming.OneDayBefore      => "Due tomorrow",
            NotificationTiming.OneHourBefore     => "Due in 1 hour",
            NotificationTiming.FiveMinutesBefore => "Due in 5 minutes",
            NotificationTiming.AtDeadline        => "Deadline reached",
            NotificationTiming.OneDayAfter       => "1 day overdue",
            _                                    => "Reminder"
        };

        DeadlineText = task.Deadline.HasValue
            ? task.Deadline.Value.ToString("ddd d MMM, HH:mm")
            : "";
    }

    /// <summary>Called every second by the window's DispatcherTimer.</summary>
    public void Tick()
    {
        SecondsLeft     = Math.Max(0, SecondsLeft - 1);
        DismissProgress = SecondsLeft * 100.0 / TotalSeconds;
        if (SecondsLeft <= 0)
            CloseRequested?.Invoke();
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task Snooze10Async()
    {
        await _notifications.SnoozeAsync(TaskId, Timing, TimeSpan.FromMinutes(10));
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private async Task Snooze1HrAsync()
    {
        await _notifications.SnoozeAsync(TaskId, Timing, TimeSpan.FromHours(1));
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private async Task DismissAsync()
    {
        await _notifications.DismissAsync(TaskId, Timing);
        CloseRequested?.Invoke();
    }
}
