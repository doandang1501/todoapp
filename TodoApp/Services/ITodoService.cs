using TodoApp.Core.Enums;
using TodoApp.Core.Models;

namespace TodoApp.Services;

/// <summary>
/// CRUD + query operations for TodoItem.
/// All methods are thread-safe; events are raised on the calling thread.
/// </summary>
public interface ITodoService
{
    // ── CRUD ────────────────────────────────────────────────────────────────
    Task<List<TodoItem>> GetAllAsync();
    Task<TodoItem?> GetByIdAsync(Guid id);
    Task<TodoItem> CreateAsync(TodoItem item);
    Task UpdateAsync(TodoItem item);
    Task DeleteAsync(Guid id);

    // ── State transitions ────────────────────────────────────────────────────
    Task CompleteAsync(Guid id);
    Task UncompleteAsync(Guid id);
    Task ArchiveAsync(Guid id);
    Task UnarchiveAsync(Guid id);

    // ── Bulk / ordering ──────────────────────────────────────────────────────
    Task ReorderAsync(IEnumerable<Guid> orderedIds);
    Task DeleteCompletedAsync();

    // ── Query ────────────────────────────────────────────────────────────────
    Task<List<TodoItem>> GetByStatusAsync(TodoStatus status);
    Task<List<TodoItem>> GetByPriorityAsync(Priority priority);
    Task<List<TodoItem>> GetDueTodayAsync();
    Task<List<TodoItem>> GetOverdueAsync();
    Task<List<TodoItem>> SearchAsync(string query);
    Task<List<TodoItem>> GetByTagAsync(string tag);

    // ── Events ───────────────────────────────────────────────────────────────
    event EventHandler<TodoItem>  TaskCreated;
    event EventHandler<TodoItem>  TaskUpdated;
    event EventHandler<Guid>      TaskDeleted;
    event EventHandler<TodoItem>  TaskCompleted;
    event EventHandler            TasksChanged;   // any write operation
}
