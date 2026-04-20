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

public partial class TaskListViewModel : ViewModelBase
{
    private readonly ITodoService              _todos;
    private readonly ISoundService             _sound;
    private readonly ILogger<TaskListViewModel> _logger;

    // ── Display collections ───────────────────────────────────────────────────

    /// <summary>Filtered + sorted items bound to the list.</summary>
    public ObservableCollection<TaskRowViewModel> FilteredTasks { get; } = new();

    // ── State ─────────────────────────────────────────────────────────────────

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(PendingCount))]
    private string _searchQuery = "";

    [ObservableProperty] private string _activeFilter = "All";
    [ObservableProperty] private string _activeSortField = "Priority";
    [ObservableProperty] private bool   _sortAscending = true;
    [ObservableProperty] private string _emptyStateMessage = "No tasks yet. Press + to add one!";

    private List<TodoItem> _allTasks = new();

    // ── Summary counts ────────────────────────────────────────────────────────

    public int TotalCount   => _allTasks.Count(t => t.Status != TodoStatus.Archived);
    public int PendingCount => _allTasks.Count(t => t.Status is TodoStatus.Todo or TodoStatus.InProgress);
    public int OverdueCount => _allTasks.Count(t => t.IsOverdue);
    public int TodayCount   => _allTasks.Count(t => t.IsToday && t.Status != TodoStatus.Done);

    // ── Constructor ───────────────────────────────────────────────────────────

    public TaskListViewModel(
        ITodoService todos,
        ISoundService sound,
        ILogger<TaskListViewModel> logger)
    {
        _todos  = todos;
        _sound  = sound;
        _logger = logger;

        _todos.TasksChanged += (_, _) => Application.Current?.Dispatcher
            .InvokeAsync(async () => await LoadTasksAsync());
    }

    // ── Init ─────────────────────────────────────────────────────────────────

    public override async Task InitializeAsync()
    {
        await RunBusyAsync(LoadTasksAsync, "Loading tasks…");
    }

    private async Task LoadTasksAsync()
    {
        _allTasks = await _todos.GetAllAsync();
        ApplyFilter();
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(OverdueCount));
        OnPropertyChanged(nameof(TodayCount));
    }

    // ── Filtering & sorting ───────────────────────────────────────────────────

    partial void OnSearchQueryChanged(string value)    => ApplyFilter();
    partial void OnActiveFilterChanged(string value)   => ApplyFilter();
    partial void OnActiveSortFieldChanged(string value) => ApplyFilter();
    partial void OnSortAscendingChanged(bool value)    => ApplyFilter();

    private void ApplyFilter()
    {
        var q = SearchQuery.Trim();

        IEnumerable<TodoItem> view = ActiveFilter switch
        {
            "Today"   => _allTasks.Where(t => t.IsToday && t.Status != TodoStatus.Archived),
            "Overdue" => _allTasks.Where(t => t.IsOverdue),
            "Pinned"  => _allTasks.Where(t => t.IsPinned && t.Status != TodoStatus.Archived),
            "High"    => _allTasks.Where(t => t.Priority is Priority.High or Priority.Critical
                                           && t.Status   != TodoStatus.Archived),
            "Done"    => _allTasks.Where(t => t.Status == TodoStatus.Done),
            _         => _allTasks.Where(t => t.Status != TodoStatus.Archived)
        };

        if (!string.IsNullOrEmpty(q))
            view = view.Where(t =>
                t.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                t.Tags.Any(tag => tag.Contains(q, StringComparison.OrdinalIgnoreCase)));

        // Pinned first, then done tasks last (unless filter=Done), then user sort
        if (ActiveFilter != "Done")
            view = view.Where(t => t.Status != TodoStatus.Done)
                       .Concat(view.Where(t => t.Status == TodoStatus.Done));

        view = (ActiveSortField, SortAscending) switch
        {
            ("Priority",  false) => view.OrderByDescending(t => t.IsPinned).ThenByDescending(t => (int)t.Priority),
            ("Priority",  true)  => view.OrderByDescending(t => t.IsPinned).ThenBy(t => (int)t.Priority),
            ("Deadline",  true)  => view.OrderByDescending(t => t.IsPinned).ThenBy(t => t.Deadline ?? DateTime.MaxValue),
            ("Deadline",  false) => view.OrderByDescending(t => t.IsPinned).ThenByDescending(t => t.Deadline ?? DateTime.MinValue),
            ("Created",   true)  => view.OrderByDescending(t => t.IsPinned).ThenBy(t => t.CreatedAt),
            ("Created",   false) => view.OrderByDescending(t => t.IsPinned).ThenByDescending(t => t.CreatedAt),
            ("Title",     true)  => view.OrderByDescending(t => t.IsPinned).ThenBy(t => t.Title),
            ("Title",     false) => view.OrderByDescending(t => t.IsPinned).ThenByDescending(t => t.Title),
            _                    => view.OrderByDescending(t => t.IsPinned).ThenByDescending(t => (int)t.Priority)
        };

        var materialized = view.ToList();

        EmptyStateMessage = ActiveFilter switch
        {
            "Today"   => TodayCount == 0 ? "No tasks due today!" : "",
            "Overdue" => OverdueCount == 0 ? "No overdue tasks!" : "",
            _         => materialized.Count == 0 ? "No tasks found. Press + to add one!" : ""
        };

        // Sync ObservableCollection (reuse existing VMs to avoid flicker)
        var existing = FilteredTasks.ToDictionary(r => r.Id);
        FilteredTasks.Clear();
        foreach (var item in materialized)
        {
            if (existing.TryGetValue(item.Id, out var row))
            {
                row.Update(item);
                FilteredTasks.Add(row);
            }
            else
            {
                FilteredTasks.Add(new TaskRowViewModel(item));
            }
        }
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void SetFilter(string filter) => ActiveFilter = filter;

    [RelayCommand]
    private void SetSort(string field)
    {
        if (ActiveSortField == field) SortAscending = !SortAscending;
        else                          ActiveSortField = field;
    }

    [RelayCommand]
    private async Task AddTaskAsync()
    {
        var vm     = new TaskDetailViewModel();
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
        var dialog = new Views.ConfirmDeleteDialog(row.Title)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            await _todos.DeleteAsync(row.Id);
            await LoadTasksAsync();
        }
    }

    [RelayCommand]
    private async Task TogglePinAsync(TaskRowViewModel row)
    {
        row.Item.IsPinned = !row.Item.IsPinned;
        await _todos.UpdateAsync(row.Item);
        await LoadTasksAsync();
    }

    /// <summary>
    /// Called by ListBoxReorderBehavior when the user drops a task onto another.
    /// Swaps positions in the visible list, then persists the new DisplayOrder.
    /// </summary>
    [RelayCommand]
    private async Task ReorderTaskAsync((object from, object to) args)
    {
        if (args.from is not TaskRowViewModel fromVm || args.to is not TaskRowViewModel toVm) return;

        var fromIdx = FilteredTasks.IndexOf(fromVm);
        var toIdx   = FilteredTasks.IndexOf(toVm);
        if (fromIdx < 0 || toIdx < 0 || fromIdx == toIdx) return;

        FilteredTasks.Move(fromIdx, toIdx);

        var orderedIds = FilteredTasks.Select(t => t.Id);
        await _todos.ReorderAsync(orderedIds);
    }
}
