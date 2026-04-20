using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TodoApp.Core.Enums;
using TodoApp.Core.Models;
using TodoApp.Data;

namespace TodoApp.Services;

/// <summary>
/// Background service that wakes up on a configurable interval and checks
/// whether any task notification window is now active.  When a notification
/// is due it fires <see cref="NotificationTriggered"/> on the UI dispatcher
/// so WPF code can safely show a toast.
/// </summary>
public sealed class NotificationService : BackgroundService, INotificationService
{
    public event EventHandler<NotificationEventArgs>? NotificationTriggered;
    public int PendingCount { get; private set; }

    private readonly ITodoService              _todos;
    private readonly IAppDataStore             _store;
    private readonly ISoundService             _sound;
    private readonly IEmailService             _email;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ITodoService todos,
        IAppDataStore store,
        ISoundService sound,
        IEmailService email,
        ILogger<NotificationService> logger)
    {
        _todos  = todos;
        _store  = store;
        _sound  = sound;
        _email  = email;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Short initial delay so the app finishes loading first
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var settings = await _store.GetSettingsAsync();
                if (settings.Notifications.Enabled)
                    await CheckNotificationsAsync(stoppingToken);

                var interval = TimeSpan.FromSeconds(
                    Math.Max(10, settings.Notifications.CheckIntervalSeconds));
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification check loop.");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task CheckNotificationsAsync(CancellationToken ct)
    {
        var settings = await _store.GetSettingsAsync();
        var now      = DateTime.Now;
        var tasks    = await _todos.GetAllAsync();
        int pending  = 0;

        // ── Focus mode suppression ────────────────────────────────────────────
        bool suppressToasts = settings.FocusMode.IsActive
                              && settings.FocusMode.SuppressToastNotifications;
        bool suppressSounds = settings.FocusMode.IsActive
                              && settings.FocusMode.SuppressSounds;

        foreach (var task in tasks)
        {
            ct.ThrowIfCancellationRequested();

            if (task.Status is TodoStatus.Done or TodoStatus.Archived) continue;
            if (!task.Deadline.HasValue)                                continue;
            if (task.NotificationTimings == NotificationTiming.None)   continue;

            var deadline = task.Deadline.Value;

            foreach (NotificationTiming timing in Enum.GetValues<NotificationTiming>())
            {
                if (timing == NotificationTiming.None)         continue;
                if (!task.NotificationTimings.HasFlag(timing)) continue;

                var window = GetNotificationWindow(deadline, timing);
                if (window is null) continue;

                if (now < window.Value.start || now > window.Value.end) continue;

                // Check whether already sent or snoozed
                var record = task.NotificationHistory
                    .FirstOrDefault(r => r.Timing == timing);

                if (record is not null)
                {
                    if (record.IsSent)   continue;
                    if (record.IsSnoozed && now < record.SnoozedUntil) continue;
                }

                // Mark as sent in history
                await MarkSentAsync(task, timing, now);
                pending++;

                var args = new NotificationEventArgs { Task = task, Timing = timing };

                // ── Toast (respects focus suppression) ───────────────────────
                if (!suppressToasts)
                    FireOnDispatcher(task, timing);

                // ── Sound (respects focus suppression) ───────────────────────
                if (!suppressSounds)
                    _ = _sound.PlayPriorityAsync(task.Priority);

                // ── Email (fires regardless of focus mode — background channel) ─
                if (settings.Email.Enabled)
                    _ = TrySendEmailAsync(task, args, settings, timing);
            }
        }

        PendingCount = pending;
    }

    private async Task TrySendEmailAsync(
        TodoItem task, NotificationEventArgs args,
        Core.Models.Settings.AppSettings settings,
        NotificationTiming timing)
    {
        // Check per-timing email flags
        bool shouldSend = timing switch
        {
            NotificationTiming.OneDayBefore    => settings.Email.SendOneDayBefore,
            NotificationTiming.OneHourBefore   => settings.Email.SendOneHourBefore,
            NotificationTiming.FiveMinutesBefore => settings.Email.SendFiveMinutesBefore,
            _                                  => false
        };

        if (!shouldSend) return;

        try
        {
            var sent = await _email.SendNotificationAsync(task, args, settings.Email);
            if (!sent)
                _logger.LogWarning("Email notification failed for task {Id} ({Timing})", task.Id, timing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending email notification for task {Id}", task.Id);
        }
    }

    private static (DateTime start, DateTime end)?
        GetNotificationWindow(DateTime deadline, NotificationTiming timing)
    {
        return timing switch
        {
            NotificationTiming.OneDayBefore     => (deadline.AddHours(-24), deadline.AddHours(-23)),
            NotificationTiming.OneHourBefore    => (deadline.AddMinutes(-60), deadline.AddMinutes(-45)),
            NotificationTiming.FiveMinutesBefore=> (deadline.AddMinutes(-5),  deadline),
            NotificationTiming.AtDeadline       => (deadline,                 deadline.AddMinutes(15)),
            NotificationTiming.OneDayAfter      => (deadline.AddHours(24),    deadline.AddHours(25)),
            _                                   => null
        };
    }

    private async Task MarkSentAsync(TodoItem task, NotificationTiming timing, DateTime now)
    {
        var record = task.NotificationHistory.FirstOrDefault(r => r.Timing == timing);
        if (record is null)
        {
            record = new NotificationRecord
            {
                Id        = Guid.NewGuid(),
                Timing    = timing,
                ScheduledAt = now
            };
            task.NotificationHistory.Add(record);
        }

        record.SentAt  = now;

        try { await _todos.UpdateAsync(task); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist notification record for task {Id}", task.Id);
        }
    }

    private void FireOnDispatcher(TodoItem task, NotificationTiming timing)
    {
        var app = Application.Current;
        if (app is null) return;

        app.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
        {
            try
            {
                NotificationTriggered?.Invoke(this, new NotificationEventArgs
                {
                    Task   = task,
                    Timing = timing
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error firing NotificationTriggered event.");
            }
        });
    }

    // ── INotificationService methods ─────────────────────────────────────────

    public async Task SnoozeAsync(Guid taskId, NotificationTiming timing, TimeSpan duration)
    {
        var task = await _todos.GetByIdAsync(taskId);
        if (task is null) return;

        var record = task.NotificationHistory.FirstOrDefault(r => r.Timing == timing);
        if (record is null)
        {
            record = new NotificationRecord { Id = Guid.NewGuid(), Timing = timing };
            task.NotificationHistory.Add(record);
        }

        record.SnoozedUntil = DateTime.Now.Add(duration);
        record.SentAt       = null; // reset so it fires again after snooze

        await _todos.UpdateAsync(task);
        _logger.LogInformation("Snoozed notification for task {Id} ({Timing}) until {Until:g}",
            taskId, timing, record.SnoozedUntil);
    }

    public async Task DismissAsync(Guid taskId, NotificationTiming timing)
    {
        var task = await _todos.GetByIdAsync(taskId);
        if (task is null) return;

        var record = task.NotificationHistory.FirstOrDefault(r => r.Timing == timing)
                     ?? new NotificationRecord { Id = Guid.NewGuid(), Timing = timing };

        if (!task.NotificationHistory.Contains(record))
            task.NotificationHistory.Add(record);

        record.SentAt       = DateTime.Now;
        record.SnoozedUntil = null;

        await _todos.UpdateAsync(task);
    }
}
