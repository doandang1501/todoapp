using System.Net.Http;
using System.Text.RegularExpressions;
using TodoApp.Core.Models;
using TodoApp.Data;

namespace TodoApp.Services;

/// <summary>
/// Manages the Watch-Later list.  Thumbnails are fetched via Open-Graph meta tags.
/// </summary>
public sealed class WatchLaterService : IWatchLaterService
{
    private readonly IAppDataStore _store;

    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(6),
    };

    public event EventHandler? ItemsChanged;

    public WatchLaterService(IAppDataStore store)
    {
        _store = store;
        _http.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (compatible; TodoApp thumbnail-fetcher)");
    }

    // ── Reads ─────────────────────────────────────────────────────────────────

    public async Task<List<WatchLaterItem>> GetAllAsync()
        => await _store.GetWatchLaterAsync();

    // ── Writes ────────────────────────────────────────────────────────────────

    public async Task<WatchLaterItem> CreateAsync(
        string title, string url, string notes, List<string> tags)
    {
        // Fetch OG thumbnail for the explicit URL
        string? thumbUrl = null;
        if (!string.IsNullOrWhiteSpace(url))
            thumbUrl = await FetchOgImageAsync(url.Trim());

        // Derive title if not supplied
        string effectiveTitle = !string.IsNullOrWhiteSpace(title)
            ? title.Trim()
            : !string.IsNullOrWhiteSpace(url) ? Truncate(url, 60)
            : Truncate(notes, 60);

        var item = new WatchLaterItem
        {
            Title        = effectiveTitle,
            Content      = notes.Trim(),  // kept for backward compat / card display
            Notes        = notes.Trim(),
            Url          = url.Trim(),
            ThumbnailUrl = thumbUrl,
            Tags         = tags,
        };

        var items = await _store.GetWatchLaterAsync();
        items.Add(item);
        await _store.SaveWatchLaterAsync(items);
        ItemsChanged?.Invoke(this, EventArgs.Empty);
        return item;
    }

    public async Task DeleteAsync(Guid id)
    {
        var items = await _store.GetWatchLaterAsync();
        items.RemoveAll(i => i.Id == id);
        await _store.SaveWatchLaterAsync(items);
        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max] + "…";

    private static async Task<string?> FetchOgImageAsync(string url)
    {
        try
        {
            var html = await _http.GetStringAsync(url).ConfigureAwait(false);

            var m = Regex.Match(
                html,
                @"<meta[^>]+property=[""']og:image[""'][^>]+content=[""']([^""']+)[""']",
                RegexOptions.IgnoreCase);

            if (!m.Success)
                m = Regex.Match(
                    html,
                    @"<meta[^>]+content=[""']([^""']+)[""'][^>]+property=[""']og:image[""']",
                    RegexOptions.IgnoreCase);

            return m.Success ? m.Groups[1].Value.Trim() : null;
        }
        catch
        {
            return null;
        }
    }
}
