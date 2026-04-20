using Microsoft.Extensions.Logging;
using TodoApp.Core.Models;
using TodoApp.Core.Models.Settings;

namespace TodoApp.Data;

/// <summary>
/// Concrete implementation of <see cref="IAppDataStore"/>.
/// Maintains an in-memory write-through cache: reads are fast after first load;
/// every write flushes to disk immediately via the underlying repositories.
/// </summary>
public sealed class AppDataStore : IAppDataStore
{
    // ── Repositories ──────────────────────────────────────────────────────────
    private readonly JsonRepository<TodoItem>           _todoRepo;
    private readonly JsonRepository<StickyNote>         _stickyRepo;
    private readonly JsonRepository<WatchLaterItem>     _watchLaterRepo;
    private readonly JsonRepository<Label>              _labelRepo;
    private readonly JsonRepository<Goal>               _goalRepo;
    private readonly SingleObjectRepository<AppSettings> _settingsRepo;

    // ── In-memory caches ──────────────────────────────────────────────────────
    private List<TodoItem>?       _cachedTodos;
    private List<StickyNote>?     _cachedSticky;
    private List<WatchLaterItem>? _cachedWatchLater;
    private List<Label>?          _cachedLabels;
    private List<Goal>?           _cachedGoals;
    private AppSettings?          _cachedSettings;

    public AppDataStore(ILoggerFactory loggerFactory)
    {
        var todoLogger       = loggerFactory.CreateLogger<JsonRepository<TodoItem>>();
        var stickyLogger     = loggerFactory.CreateLogger<JsonRepository<StickyNote>>();
        var watchLaterLogger = loggerFactory.CreateLogger<JsonRepository<WatchLaterItem>>();
        var labelLogger      = loggerFactory.CreateLogger<JsonRepository<Label>>();
        var settingsLogger   = loggerFactory.CreateLogger<SingleObjectRepository<AppSettings>>();

        _todoRepo       = new JsonRepository<TodoItem>(DataPaths.TodosFile,        todoLogger);
        _stickyRepo     = new JsonRepository<StickyNote>(DataPaths.StickyNotesFile, stickyLogger);
        _watchLaterRepo = new JsonRepository<WatchLaterItem>(DataPaths.WatchLaterFile, watchLaterLogger);
        _labelRepo      = new JsonRepository<Label>(DataPaths.LabelsFile,          labelLogger);
        _goalRepo       = new JsonRepository<Goal>(DataPaths.GoalsFile,            loggerFactory.CreateLogger<JsonRepository<Goal>>());
        _settingsRepo   = new SingleObjectRepository<AppSettings>(DataPaths.SettingsFile, settingsLogger);
    }

    // ── Todos ─────────────────────────────────────────────────────────────────

    public async Task<List<TodoItem>> GetTodosAsync(CancellationToken ct = default)
    {
        _cachedTodos ??= await _todoRepo.GetAllAsync(ct).ConfigureAwait(false);
        return _cachedTodos;
    }

    public async Task SaveTodosAsync(List<TodoItem> todos, CancellationToken ct = default)
    {
        _cachedTodos = todos;
        await _todoRepo.SaveAllAsync(todos, ct).ConfigureAwait(false);
    }

    // ── Sticky Notes ──────────────────────────────────────────────────────────

    public async Task<List<StickyNote>> GetStickyNotesAsync(CancellationToken ct = default)
    {
        _cachedSticky ??= await _stickyRepo.GetAllAsync(ct).ConfigureAwait(false);
        return _cachedSticky;
    }

    public async Task SaveStickyNotesAsync(List<StickyNote> notes, CancellationToken ct = default)
    {
        _cachedSticky = notes;
        await _stickyRepo.SaveAllAsync(notes, ct).ConfigureAwait(false);
    }

    // ── Settings ──────────────────────────────────────────────────────────────

    public async Task<AppSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        _cachedSettings ??= await _settingsRepo.LoadAsync(ct).ConfigureAwait(false);
        return _cachedSettings;
    }

    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken ct = default)
    {
        _cachedSettings = settings;
        await _settingsRepo.SaveAsync(settings, ct).ConfigureAwait(false);
    }

    // ── Watch Later ───────────────────────────────────────────────────────────

    public async Task<List<WatchLaterItem>> GetWatchLaterAsync(CancellationToken ct = default)
    {
        _cachedWatchLater ??= await _watchLaterRepo.GetAllAsync(ct).ConfigureAwait(false);
        return _cachedWatchLater;
    }

    public async Task SaveWatchLaterAsync(List<WatchLaterItem> items, CancellationToken ct = default)
    {
        _cachedWatchLater = items;
        await _watchLaterRepo.SaveAllAsync(items, ct).ConfigureAwait(false);
    }

    // ── Labels ────────────────────────────────────────────────────────────────

    public async Task<List<Label>> GetLabelsAsync(CancellationToken ct = default)
    {
        _cachedLabels ??= await _labelRepo.GetAllAsync(ct).ConfigureAwait(false);
        return _cachedLabels;
    }

    public async Task SaveLabelsAsync(List<Label> labels, CancellationToken ct = default)
    {
        _cachedLabels = labels;
        await _labelRepo.SaveAllAsync(labels, ct).ConfigureAwait(false);
    }

    // ── Goals ─────────────────────────────────────────────────────────────────

    public async Task<List<Goal>> GetGoalsAsync(CancellationToken ct = default)
    {
        _cachedGoals ??= await _goalRepo.GetAllAsync(ct).ConfigureAwait(false);
        return _cachedGoals;
    }

    public async Task SaveGoalsAsync(List<Goal> goals, CancellationToken ct = default)
    {
        _cachedGoals = goals;
        await _goalRepo.SaveAllAsync(goals, ct).ConfigureAwait(false);
    }

    // ── Cache control ─────────────────────────────────────────────────────────

    public void InvalidateCache()
    {
        _cachedTodos      = null;
        _cachedSticky     = null;
        _cachedWatchLater = null;
        _cachedLabels     = null;
        _cachedGoals      = null;
        _cachedSettings   = null;
    }
}
