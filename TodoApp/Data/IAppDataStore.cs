using TodoApp.Core.Models;
using TodoApp.Core.Models.Settings;

namespace TodoApp.Data;

/// <summary>
/// Abstraction over all persisted data.
/// Consumers never touch repositories directly — only this interface.
/// </summary>
public interface IAppDataStore
{
    // ── Todos ─────────────────────────────────────────────────────────────────
    Task<List<TodoItem>>  GetTodosAsync(CancellationToken ct = default);
    Task                  SaveTodosAsync(List<TodoItem> todos, CancellationToken ct = default);

    // ── Sticky Notes ──────────────────────────────────────────────────────────
    Task<List<StickyNote>> GetStickyNotesAsync(CancellationToken ct = default);
    Task                   SaveStickyNotesAsync(List<StickyNote> notes, CancellationToken ct = default);

    // ── Watch Later ───────────────────────────────────────────────────────────
    Task<List<WatchLaterItem>> GetWatchLaterAsync(CancellationToken ct = default);
    Task                       SaveWatchLaterAsync(List<WatchLaterItem> items, CancellationToken ct = default);

    // ── Labels ────────────────────────────────────────────────────────────────
    Task<List<Label>> GetLabelsAsync(CancellationToken ct = default);
    Task              SaveLabelsAsync(List<Label> labels, CancellationToken ct = default);

    // ── Goals ─────────────────────────────────────────────────────────────────
    Task<List<Goal>> GetGoalsAsync(CancellationToken ct = default);
    Task             SaveGoalsAsync(List<Goal> goals, CancellationToken ct = default);

    // ── Settings ──────────────────────────────────────────────────────────────
    Task<AppSettings> GetSettingsAsync(CancellationToken ct = default);
    Task              SaveSettingsAsync(AppSettings settings, CancellationToken ct = default);

    // ── Cache control ─────────────────────────────────────────────────────────
    /// <summary>
    /// Discard in-memory caches so the next read loads fresh data from disk.
    /// Called after backup-restore or external import.
    /// </summary>
    void InvalidateCache();
}
