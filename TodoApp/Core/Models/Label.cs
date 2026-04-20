namespace TodoApp.Core.Models;

/// <summary>
/// A reusable coloured label that can be attached to tasks, watch-later items,
/// and quick notes.  Persisted in labels.json.
/// </summary>
public class Label
{
    public Guid   Id    { get; set; } = Guid.NewGuid();
    public string Name  { get; set; } = string.Empty;
    /// <summary>Hex colour string, e.g. "#E91E63".</summary>
    public string Color { get; set; } = "#E91E63";
}
