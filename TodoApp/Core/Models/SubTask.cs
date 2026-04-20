namespace TodoApp.Core.Models;

/// <summary>
/// A child checklist item belonging to a TodoItem.
/// Progress % on the parent is auto-computed from SubTask completion.
/// </summary>
public class SubTask
{
    public Guid Id           { get; set; } = Guid.NewGuid();
    public string Title      { get; set; } = string.Empty;
    public bool IsCompleted  { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int DisplayOrder  { get; set; }
}
