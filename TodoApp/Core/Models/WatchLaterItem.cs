namespace TodoApp.Core.Models;

/// <summary>
/// Represents a saved "watch later" entry — a URL, snippet, or note the user
/// wants to revisit later.  Persisted in watchlater.json.
/// </summary>
public class WatchLaterItem
{
    public Guid     Id           { get; set; } = Guid.NewGuid();
    public string   Title        { get; set; } = string.Empty;
    public string   Content      { get; set; } = string.Empty;

    /// <summary>
    /// The canonical URL extracted from <see cref="Content"/>, if any.
    /// </summary>
    public string?  Url          { get; set; }

    /// <summary>
    /// Open-Graph og:image URL fetched asynchronously when the item is created.
    /// May be null if the page has no OG image or if the fetch failed.
    /// </summary>
    public string?      ThumbnailUrl { get; set; }

    /// <summary>Additional text notes (markdown supported).</summary>
    public string       Notes        { get; set; } = string.Empty;

    /// <summary>Label names attached to this item.</summary>
    public List<string> Tags         { get; set; } = new();

    public DateTime     CreatedAt    { get; set; } = DateTime.Now;
}
