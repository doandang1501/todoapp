using TodoApp.Core.Enums;
using TodoApp.Core.Models;

namespace TodoApp.ViewModels;

/// <summary>
/// Represents a single day cell in the Calendar month view.
/// </summary>
public class CalendarDayViewModel
{
    public DateTime       Date           { get; }
    public bool           IsCurrentMonth { get; }
    public bool           IsToday        { get; }
    public bool           IsSelected     { get; set; }
    public List<TodoItem> Tasks          { get; }

    // Convenience computed
    public int  DayNumber    => Date.Day;
    public bool HasTasks     => Tasks.Count > 0;
    public int  TaskCount    => Tasks.Count;
    public int  OverdueCount => Tasks.Count(t => t.IsOverdue);
    public bool HasOverdue   => OverdueCount > 0;
    public int  DoneCount    => Tasks.Count(t => t.Status == TodoStatus.Done);
    public int  ActiveCount  => Tasks.Count(t => t.Status != TodoStatus.Done && t.Status != TodoStatus.Archived);

    // Max 2 task previews shown inside the cell
    public IEnumerable<TodoItem> TaskPreview => Tasks.Take(2);

    // Up to 4 priority dot colors for compact display
    public IEnumerable<string> DotColors => Tasks
        .Where(t => t.Status != TodoStatus.Done && t.Status != TodoStatus.Archived)
        .Take(4)
        .Select(t => t.Priority switch
        {
            Priority.Critical => "#F44336",
            Priority.High     => "#FF9800",
            Priority.Medium   => "#2196F3",
            Priority.Low      => "#4CAF50",
            _                 => "#9E9E9E"
        });

    public CalendarDayViewModel(DateTime date, bool isCurrentMonth, IEnumerable<TodoItem>? tasks = null)
    {
        Date           = date;
        IsCurrentMonth = isCurrentMonth;
        IsToday        = date.Date == DateTime.Today;
        Tasks          = tasks?.ToList() ?? new List<TodoItem>();
    }
}
