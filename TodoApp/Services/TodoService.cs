using Microsoft.Extensions.Logging;
using TodoApp.Core.Enums;
using TodoApp.Core.Models;
using TodoApp.Data;

namespace TodoApp.Services;

/// <summary>
/// Business-logic layer for task management.
/// Writes go through IAppDataStore; a local in-memory list is kept in sync.
/// </summary>
public sealed class TodoService : ITodoService
{
    private readonly IAppDataStore _store;
    private readonly ILogger<TodoService> _logger;

    private List<TodoItem>? _cache;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public event EventHandler<TodoItem>? TaskCreated;
    public event EventHandler<TodoItem>? TaskUpdated;
    public event EventHandler<Guid>?     TaskDeleted;
    public event EventHandler<TodoItem>? TaskCompleted;
    public event EventHandler?           TasksChanged;

    public TodoService(IAppDataStore store, ILogger<TodoService> logger)
    {
        _store  = store;
        _logger = logger;
    }

    // ── Internal cache helpers ───────────────────────────────────────────────

    private async Task<List<TodoItem>> LoadAsync()
    {
        if (_cache is not null) return _cache;
        _cache = await _store.GetTodosAsync();
        return _cache;
    }

    private async Task PersistAsync()
    {
        await _store.SaveTodosAsync(_cache!);
    }

    // ── CRUD ────────────────────────────────────────────────────────────────

    public async Task<List<TodoItem>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try   { return [.. await LoadAsync()]; }
        finally { _lock.Release(); }
    }

    public async Task<TodoItem?> GetByIdAsync(Guid id)
    {
        await _lock.WaitAsync();
        try   { return (await LoadAsync()).FirstOrDefault(t => t.Id == id); }
        finally { _lock.Release(); }
    }

    public async Task<TodoItem> CreateAsync(TodoItem item)
    {
        await _lock.WaitAsync();
        try
        {
            var list = await LoadAsync();
            item.CreatedAt  = DateTime.Now;
            item.UpdatedAt  = DateTime.Now;
            item.DisplayOrder = list.Count > 0
                ? list.Max(t => t.DisplayOrder) + 1
                : 0;
            list.Add(item);
            await PersistAsync();
            _logger.LogInformation("Task created: {Id} '{Title}'", item.Id, item.Title);
        }
        finally { _lock.Release(); }

        TaskCreated?.Invoke(this, item);
        TasksChanged?.Invoke(this, EventArgs.Empty);
        return item;
    }

    public async Task UpdateAsync(TodoItem item)
    {
        await _lock.WaitAsync();
        try
        {
            var list  = await LoadAsync();
            var idx   = list.FindIndex(t => t.Id == item.Id);
            if (idx < 0) throw new KeyNotFoundException($"Task {item.Id} not found.");
            item.UpdatedAt = DateTime.Now;
            list[idx] = item;
            await PersistAsync();
        }
        finally { _lock.Release(); }

        TaskUpdated?.Invoke(this, item);
        TasksChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            var list = await LoadAsync();
            var removed = list.RemoveAll(t => t.Id == id);
            if (removed == 0) return;
            await PersistAsync();
        }
        finally { _lock.Release(); }

        TaskDeleted?.Invoke(this, id);
        TasksChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── State transitions ────────────────────────────────────────────────────

    public async Task CompleteAsync(Guid id)
    {
        TodoItem? item;
        await _lock.WaitAsync();
        try
        {
            var list = await LoadAsync();
            item = list.FirstOrDefault(t => t.Id == id)
                   ?? throw new KeyNotFoundException($"Task {id} not found.");
            item.Status      = TodoStatus.Done;
            item.CompletedAt = DateTime.Now;
            item.UpdatedAt   = DateTime.Now;

            // Complete all subtasks too
            foreach (var sub in item.SubTasks)
            {
                if (!sub.IsCompleted)
                {
                    sub.IsCompleted  = true;
                    sub.CompletedAt  = DateTime.Now;
                }
            }
            await PersistAsync();
        }
        finally { _lock.Release(); }

        TaskCompleted?.Invoke(this, item);
        TaskUpdated?.Invoke(this, item);
        TasksChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task UncompleteAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            var list = await LoadAsync();
            var item = list.FirstOrDefault(t => t.Id == id)
                       ?? throw new KeyNotFoundException($"Task {id} not found.");
            item.Status      = TodoStatus.Todo;
            item.CompletedAt = null;
            item.UpdatedAt   = DateTime.Now;
            await PersistAsync();
        }
        finally { _lock.Release(); }

        TasksChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task ArchiveAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            var list = await LoadAsync();
            var item = list.FirstOrDefault(t => t.Id == id)
                       ?? throw new KeyNotFoundException($"Task {id} not found.");
            item.Status    = TodoStatus.Archived;
            item.UpdatedAt = DateTime.Now;
            await PersistAsync();
        }
        finally { _lock.Release(); }

        TasksChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task UnarchiveAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            var list = await LoadAsync();
            var item = list.FirstOrDefault(t => t.Id == id)
                       ?? throw new KeyNotFoundException($"Task {id} not found.");
            item.Status    = TodoStatus.Todo;
            item.UpdatedAt = DateTime.Now;
            await PersistAsync();
        }
        finally { _lock.Release(); }

        TasksChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── Bulk / ordering ──────────────────────────────────────────────────────

    public async Task ReorderAsync(IEnumerable<Guid> orderedIds)
    {
        await _lock.WaitAsync();
        try
        {
            var list  = await LoadAsync();
            var order = 0;
            foreach (var id in orderedIds)
            {
                var item = list.FirstOrDefault(t => t.Id == id);
                if (item is not null) item.DisplayOrder = order++;
            }
            await PersistAsync();
        }
        finally { _lock.Release(); }

        TasksChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task DeleteCompletedAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var list = await LoadAsync();
            list.RemoveAll(t => t.Status == TodoStatus.Done);
            await PersistAsync();
        }
        finally { _lock.Release(); }

        TasksChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── Query ────────────────────────────────────────────────────────────────

    public async Task<List<TodoItem>> GetByStatusAsync(TodoStatus status)
    {
        var all = await GetAllAsync();
        return all.Where(t => t.Status == status)
                  .OrderBy(t => t.DisplayOrder)
                  .ToList();
    }

    public async Task<List<TodoItem>> GetByPriorityAsync(Priority priority)
    {
        var all = await GetAllAsync();
        return all.Where(t => t.Priority == priority)
                  .OrderBy(t => t.DisplayOrder)
                  .ToList();
    }

    public async Task<List<TodoItem>> GetDueTodayAsync()
    {
        var all   = await GetAllAsync();
        var today = DateTime.Today;
        return all.Where(t => t.Deadline.HasValue
                           && t.Deadline.Value.Date == today
                           && t.Status != TodoStatus.Done
                           && t.Status != TodoStatus.Archived)
                  .OrderBy(t => t.Deadline)
                  .ToList();
    }

    public async Task<List<TodoItem>> GetOverdueAsync()
    {
        var all = await GetAllAsync();
        return all.Where(t => t.IsOverdue)
                  .OrderBy(t => t.Deadline)
                  .ToList();
    }

    public async Task<List<TodoItem>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return await GetAllAsync();
        var all = await GetAllAsync();
        var q   = query.Trim();
        return all.Where(t =>
                t.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (t.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                t.Tags.Any(tag => tag.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(t => t.DisplayOrder)
            .ToList();
    }

    public async Task<List<TodoItem>> GetByTagAsync(string tag)
    {
        var all = await GetAllAsync();
        return all.Where(t => t.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                  .OrderBy(t => t.DisplayOrder)
                  .ToList();
    }
}
