using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TodoApp.Core.Enums;
using TodoApp.Core.Models;
using TodoApp.Services;
using TodoApp.ViewModels.Base;
using TodoApp.Views;

namespace TodoApp.ViewModels;

public partial class KanbanViewModel : ViewModelBase
{
    private readonly ITodoService               _todos;
    private readonly ISoundService              _sound;
    private readonly ILogger<KanbanViewModel>   _logger;

    // ── Three column collections ──────────────────────────────────────────────

    public ObservableCollection<TaskRowViewModel> TodoItems       { get; } = new();
    public ObservableCollection<TaskRowViewModel> InProgressItems { get; } = new();
    public ObservableCollection<TaskRowViewModel> DoneItems       { get; } = new();

    // ── Column counts ─────────────────────────────────────────────────────────

    public int TodoCount       => TodoItems.Count;
    public int InProgressCount => InProgressItems.Count;
    public int DoneCount       => DoneItems.Count;

    // ── State ─────────────────────────────────────────────────────────────────

    [ObservableProperty] private string _newTaskTitle = "";

    // ── Private cache ─────────────────────────────────────────────────────────

    private List<TodoItem> _allTasks = new();

    // ── Constructor ───────────────────────────────────────────────────────────

    public KanbanViewModel(
        ITodoService todos,
        ISoundService sound,
        ILogger<KanbanViewModel> logger)
    {
        _todos  = todos;
        _sound  = sound;
        _logger = logger;

        _todos.TasksChanged += (_, _) =>
            Application.Current?.Dispatcher.InvokeAsync(async () => await LoadTasksAsync());
    }

    // ── Init ──────────────────────────────────────────────────────────────────

    public override async Task InitializeAsync()
        => await RunBusyAsync(LoadTasksAsync, "Loading board…");

    private async Task LoadTasksAsync()
    {
        _allTasks = await _todos.GetAllAsync();
        RebuildColumns();
    }

    private void RebuildColumns()
    {
        var active = _allTasks
            .Where(t => t.Status != TodoStatus.Archived)
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => (int)t.Priority)
            .ThenBy(t => t.Deadline ?? DateTime.MaxValue)
            .ToList();

        SyncColumn(TodoItems,       active.Where(t => t.Status == TodoStatus.Todo));
        SyncColumn(InProgressItems, active.Where(t => t.Status == TodoStatus.InProgress));
        SyncColumn(DoneItems,       active.Where(t => t.Status == TodoStatus.Done));

        OnPropertyChanged(nameof(TodoCount));
        OnPropertyChanged(nameof(InProgressCount));
        OnPropertyChanged(nameof(DoneCount));
    }

    private static void SyncColumn(
        ObservableCollection<TaskRowViewModel> col,
        IEnumerable<TodoItem> items)
    {
        var existing = col.ToDictionary(r => r.Id);
        col.Clear();
        foreach (var item in items)
        {
            if (existing.TryGetValue(item.Id, out var row)) { row.Update(item); col.Add(row); }
            else col.Add(new TaskRowViewModel(item));
        }
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>Drag-and-drop drop target — move task to a new status column.</summary>
    [RelayCommand]
    private async Task MoveTaskAsync((Guid taskId, string targetColumn) args)
    {
        var (taskId, targetColumn) = args;
        var item = _allTasks.FirstOrDefault(t => t.Id == taskId);
        if (item is null) return;

        var newStatus = targetColumn switch
        {
            "Todo"       => TodoStatus.Todo,
            "InProgress" => TodoStatus.InProgress,
            "Done"       => TodoStatus.Done,
            _            => item.Status
        };

        if (item.Status == newStatus) return;

        var wasCompleted = item.Status == TodoStatus.Done;
        item.Status = newStatus;

        if (newStatus == TodoStatus.Done && !wasCompleted)
        {
            item.CompletedAt = DateTime.Now;
            await _sound.PlayCompletionAsync();
        }
        else if (newStatus != TodoStatus.Done)
        {
            item.CompletedAt = null;
        }

        await _todos.UpdateAsync(item);
        RebuildColumns();
    }

    [RelayCommand]
    private async Task AddTaskAsync(string? statusHint)
    {
        var vm     = new TaskDetailViewModel();
        if (statusHint != null)
        {
            vm.StatusTodo       = statusHint == "Todo";
            vm.StatusInProgress = statusHint == "InProgress";
            vm.StatusDone       = statusHint == "Done";
        }

        var dialog = new TaskDetailDialog(vm);
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() == true)
        {
            await RunBusyAsync(async () =>
            {
                var item = vm.BuildNewItem();
                await _todos.CreateAsync(item);
                await LoadTasksAsync();
            }, "Saving…");
        }
    }

    [RelayCommand]
    private async Task EditTaskAsync(TaskRowViewModel row)
    {
        var vm     = new TaskDetailViewModel(row.Item);
        var dialog = new TaskDetailDialog(vm);
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() == true)
        {
            await RunBusyAsync(async () =>
            {
                vm.ApplyTo(row.Item);
                await _todos.UpdateAsync(row.Item);
                await LoadTasksAsync();
            }, "Saving…");
        }
    }

    [RelayCommand]
    private async Task ToggleCompleteAsync(TaskRowViewModel row)
    {
        if (row.IsCompleted)
            await _todos.UncompleteAsync(row.Id);
        else
        {
            await _todos.CompleteAsync(row.Id);
            await _sound.PlayCompletionAsync();
        }
        await LoadTasksAsync();
    }

    [RelayCommand]
    private async Task DeleteTaskAsync(TaskRowViewModel row)
    {
        var result = MessageBox.Show(
            $"Delete \"{row.Title}\"?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _todos.DeleteAsync(row.Id);
            await LoadTasksAsync();
        }
    }
}
