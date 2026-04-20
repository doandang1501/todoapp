using System.Windows;
using Microsoft.Extensions.Logging;
using TodoApp.ViewModels;
using TodoApp.Views;

namespace TodoApp.Services;

/// <summary>
/// Listens to <see cref="INotificationService.NotificationTriggered"/> and
/// shows stacked <see cref="ToastWindow"/> popups at the bottom-right corner.
///
/// At most <see cref="MaxVisible"/> toasts are shown simultaneously.
/// Excess notifications are queued and shown as slots free up.
/// </summary>
public sealed class ToastService : IDisposable
{
    private const int MaxVisible = 3;

    private readonly INotificationService       _notifications;
    private readonly ILogger<ToastService>      _logger;
    private readonly List<ToastWindow>          _visible  = new();
    private readonly Queue<(TodoApp.Core.Models.TodoItem task, TodoApp.Core.Enums.NotificationTiming timing)>
                                                _pending  = new();

    public ToastService(
        INotificationService notifications,
        ILogger<ToastService> logger)
    {
        _notifications = notifications;
        _logger        = logger;

        _notifications.NotificationTriggered += OnNotificationTriggered;
    }

    // ── Event handler (already on UI thread via dispatcher) ───────────────────

    private void OnNotificationTriggered(object? sender, NotificationEventArgs e)
    {
        if (_visible.Count < MaxVisible)
            ShowToast(e.Task, e.Timing);
        else
            _pending.Enqueue((e.Task, e.Timing));
    }

    // ── Show a single toast ───────────────────────────────────────────────────

    private void ShowToast(
        TodoApp.Core.Models.TodoItem task,
        TodoApp.Core.Enums.NotificationTiming timing)
    {
        try
        {
            var vm     = new ToastViewModel(task, timing, _notifications);
            var window = new ToastWindow(vm);

            window.Loaded += (_, _) =>
            {
                window.SetPosition(_visible.IndexOf(window));
            };

            window.Closed += (_, _) =>
            {
                _visible.Remove(window);
                ReStackVisible();
                ShowNextPending();
            };

            _visible.Add(window);
            window.Show();

            // Re-stack all open toasts so positions remain correct
            ReStackVisible();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show toast for task {Id}", task.Id);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ReStackVisible()
    {
        var area     = SystemParameters.WorkArea;
        const double margin  = 16;
        const double spacing = 12;

        for (int i = 0; i < _visible.Count; i++)
        {
            var w = _visible[i];
            w.Left = area.Right  - w.Width - margin;
            w.Top  = area.Bottom - w.ActualHeight - margin
                     - i * (w.ActualHeight + spacing);
        }
    }

    private void ShowNextPending()
    {
        if (_pending.Count == 0) return;
        if (_visible.Count >= MaxVisible) return;

        var (task, timing) = _pending.Dequeue();
        ShowToast(task, timing);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public void Dispose()
    {
        _notifications.NotificationTriggered -= OnNotificationTriggered;
    }
}
