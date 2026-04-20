using CommunityToolkit.Mvvm.ComponentModel;
using TodoApp.Core.Enums;
using TodoApp.Core.Models;

namespace TodoApp.ViewModels;

/// <summary>
/// Lightweight display wrapper around a <see cref="TodoItem"/> for use in the task list.
/// Raises property-change notifications when the underlying item is refreshed.
/// </summary>
public partial class TaskRowViewModel : ObservableObject
{
    private TodoItem _item = null!;

    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private bool _isPinned;

    public TaskRowViewModel(TodoItem item) => Update(item);

    /// <summary>Expose the underlying item (read-only from outside).</summary>
    public TodoItem Item => _item;

    /// <summary>Replace the wrapped item and refresh all bindings.</summary>
    public void Update(TodoItem item)
    {
        _item        = item;
        IsCompleted  = item.Status == TodoStatus.Done;
        IsPinned     = item.IsPinned;
        OnPropertyChanged(string.Empty);
    }

    // ── Forwarded computed properties ─────────────────────────────────────────

    public Guid         Id               => _item.Id;
    public string       Title            => _item.Title;
    public string       Description      => _item.Description;
    public Priority     Priority         => _item.Priority;
    public TodoStatus   Status           => _item.Status;
    public List<string> Tags             => _item.Tags;
    public DateTime?    Deadline         => _item.Deadline;
    public double       Progress         => _item.ProgressPercentage;
    public int          SubTaskCount     => _item.SubTasks.Count;
    public int          SubTaskDone      => _item.SubTasks.Count(s => s.IsCompleted);
    public bool         HasSubTasks      => _item.SubTasks.Count > 0;
    public bool         IsOverdue        => _item.IsOverdue;
    public bool         IsToday          => _item.IsToday;
    public bool         IsRecurring      => _item.IsRecurring;
    public string       PriorityLabel    => _item.PriorityLabel;

    /// <summary>Human-friendly relative deadline string.</summary>
    public string DeadlineRelative
    {
        get
        {
            if (!_item.Deadline.HasValue) return string.Empty;
            var d = _item.Deadline.Value;
            var today = DateTime.Today;

            if (_item.IsOverdue)
            {
                var days = (today - d.Date).Days;
                return days == 1 ? "Overdue by 1 day"
                                 : $"Overdue by {days} days";
            }

            if (d.Date == today)               return $"Today {d:HH:mm}";
            if (d.Date == today.AddDays(1))    return $"Tomorrow {d:HH:mm}";

            var diff = (d.Date - today).Days;
            return diff <= 7
                ? $"In {diff} days — {d:MMM d}"
                : d.ToString("MMM d, yyyy");
        }
    }

    /// <summary>True when deadline is within 24 hours and not done.</summary>
    public bool IsUrgent =>
        _item.Deadline.HasValue &&
        !IsCompleted &&
        (_item.Deadline.Value - DateTime.Now).TotalHours <= 24 &&
        _item.Deadline.Value > DateTime.Now;
}
