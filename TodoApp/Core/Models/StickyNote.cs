namespace TodoApp.Core.Models;

/// <summary>
/// A free-floating always-on-top note window.
/// Position, size, and content are persisted in stickynotes.json.
/// </summary>
public class StickyNote
{
    public Guid   Id              { get; set; } = Guid.NewGuid();
    public string Content         { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = "#FFF9C4"; // Soft yellow default

    // Window geometry
    public double Left   { get; set; } = 100;
    public double Top    { get; set; } = 100;
    public double Width  { get; set; } = 260;
    public double Height { get; set; } = 200;

    public bool IsAlwaysOnTop { get; set; } = true;
    public bool IsVisible     { get; set; } = true;
    public double Opacity     { get; set; } = 1.0;

    public DateTime  CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}
