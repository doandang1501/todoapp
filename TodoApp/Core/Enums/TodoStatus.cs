namespace TodoApp.Core.Enums;

/// <summary>Lifecycle status of a task — drives both List view filtering and Kanban columns.</summary>
public enum TodoStatus
{
    Todo       = 0,
    InProgress = 1,
    Done       = 2,
    Archived   = 3
}
