using System.Text.Json.Serialization;
using TodoApp.Core.Enums;

namespace TodoApp.Core.Models;

/// <summary>
/// Root aggregate for a single task.
/// Persisted as a flat JSON object inside todos.json.
/// Computed properties ([JsonIgnore]) are derived at runtime and never serialised.
/// </summary>
public class TodoItem
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public Guid   Id          { get; set; } = Guid.NewGuid();
    public string Title       { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // ── Timestamps ────────────────────────────────────────────────────────────
    public DateTime  CreatedAt   { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt   { get; set; }
    public DateTime? Deadline    { get; set; }
    public DateTime? CompletedAt { get; set; }

    // ── Classification ────────────────────────────────────────────────────────
    public Priority   Priority { get; set; } = Priority.Medium;
    public TodoStatus Status   { get; set; } = TodoStatus.Todo;

    public List<string>  Tags     { get; set; } = new();
    public List<SubTask> SubTasks { get; set; } = new();

    // ── Display order ─────────────────────────────────────────────────────────
    /// <summary>Sort position in List view.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Sort position within a Kanban column (grouped by Status).</summary>
    public int KanbanOrder  { get; set; }

    public bool IsPinned    { get; set; }

    // ── Recurring ─────────────────────────────────────────────────────────────
    /// <summary>True on the template item; false on generated instances.</summary>
    public bool IsRecurring          { get; set; }
    public RecurringRule? RecurringRule { get; set; }

    /// <summary>Points to the template's Id when this is a generated instance.</summary>
    public Guid? RecurringParentId   { get; set; }

    // ── Notifications ─────────────────────────────────────────────────────────
    public NotificationTiming NotificationTimings { get; set; } =
        NotificationTiming.OneDayBefore |
        NotificationTiming.OneHourBefore |
        NotificationTiming.FiveMinutesBefore |
        NotificationTiming.AtDeadline;

    public List<NotificationRecord> NotificationHistory { get; set; } = new();

    // ── Computed (not persisted) ──────────────────────────────────────────────

    [JsonIgnore]
    public double ProgressPercentage
    {
        get
        {
            if (SubTasks.Count == 0)
                return Status == TodoStatus.Done ? 100.0 : 0.0;
            return (double)SubTasks.Count(s => s.IsCompleted) / SubTasks.Count * 100.0;
        }
    }

    [JsonIgnore]
    public bool IsOverdue =>
        Deadline.HasValue &&
        Deadline.Value < DateTime.Now &&
        Status != TodoStatus.Done &&
        Status != TodoStatus.Archived;

    [JsonIgnore]
    public bool IsToday =>
        Deadline.HasValue && Deadline.Value.Date == DateTime.Today;

    [JsonIgnore]
    public bool IsUpcoming =>
        Deadline.HasValue &&
        Deadline.Value.Date > DateTime.Today &&
        Deadline.Value.Date <= DateTime.Today.AddDays(7);

    [JsonIgnore]
    public string PriorityLabel => Priority switch
    {
        Priority.Low      => "Low",
        Priority.Medium   => "Medium",
        Priority.High     => "High",
        Priority.Critical => "Critical",
        _                 => Priority.ToString()
    };

    [JsonIgnore]
    public string StatusLabel => Status switch
    {
        TodoStatus.Todo       => "To Do",
        TodoStatus.InProgress => "In Progress",
        TodoStatus.Done       => "Done",
        TodoStatus.Archived   => "Archived",
        _                     => Status.ToString()
    };

    [JsonIgnore]
    public string DeadlineDisplay =>
        Deadline.HasValue ? Deadline.Value.ToString("MMM dd, yyyy HH:mm") : "No deadline";

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Creates a shallow duplicate with a new Id, reset status, and "(copy)" suffix.</summary>
    public TodoItem Duplicate() => new()
    {
        Id                 = Guid.NewGuid(),
        Title              = $"{Title} (copy)",
        Description        = Description,
        Deadline           = Deadline,
        Priority           = Priority,
        Status             = TodoStatus.Todo,
        Tags               = new List<string>(Tags),
        SubTasks           = SubTasks.Select(s => new SubTask
        {
            Id           = Guid.NewGuid(),
            Title        = s.Title,
            IsCompleted  = false,
            DisplayOrder = s.DisplayOrder
        }).ToList(),
        DisplayOrder       = DisplayOrder,
        NotificationTimings = NotificationTimings,
        CreatedAt          = DateTime.Now
    };
}
