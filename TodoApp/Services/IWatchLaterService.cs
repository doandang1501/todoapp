using TodoApp.Core.Models;

namespace TodoApp.Services;

public interface IWatchLaterService
{
    Task<List<WatchLaterItem>> GetAllAsync();

    /// <summary>
    /// Creates a new Watch-Later item.
    /// <paramref name="url"/> is an explicit link (thumbnail is fetched from it).
    /// <paramref name="notes"/> is free-form markdown text.
    /// <paramref name="tags"/> are label names attached to the item.
    /// </summary>
    Task<WatchLaterItem> CreateAsync(string title, string url, string notes, List<string> tags);

    Task DeleteAsync(Guid id);
    event EventHandler ItemsChanged;
}
