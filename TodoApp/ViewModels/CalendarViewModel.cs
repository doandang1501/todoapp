using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TodoApp.Core.Models;
using TodoApp.Services;
using TodoApp.ViewModels.Base;

namespace TodoApp.ViewModels;

/// <summary>
/// ViewModel for the Calendar page.
/// Displays a monthly grid of days; clicking a day shows its tasks in a side panel.
/// </summary>
public partial class CalendarViewModel : ViewModelBase
{
    private readonly ITodoService _todos;

    // ── Month state ──────────────────────────────────────────────────────────

    [ObservableProperty] private DateTime _currentMonth;
    [ObservableProperty] private string   _monthYearLabel = "";

    // All 42 day cells (6 rows × 7 cols) for the displayed month
    public ObservableCollection<CalendarDayViewModel> Days { get; } = new();

    // ── Selected day ─────────────────────────────────────────────────────────

    [ObservableProperty] private CalendarDayViewModel? _selectedDay;
    [ObservableProperty] private string _selectedDayLabel = "";

    public ObservableCollection<TodoItem> SelectedDayTasks { get; } = new();

    [ObservableProperty] private bool _hasSelectedDay;

    // ── Constructor ──────────────────────────────────────────────────────────

    public CalendarViewModel(ITodoService todos)
    {
        _todos = todos;
        _todos.TasksChanged += (_, _) => _ = RefreshCurrentMonthAsync();
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public override async Task InitializeAsync()
    {
        CurrentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        await LoadMonthAsync();
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task PreviousMonthAsync()
    {
        CurrentMonth = CurrentMonth.AddMonths(-1);
        await LoadMonthAsync();
    }

    [RelayCommand]
    private async Task NextMonthAsync()
    {
        CurrentMonth = CurrentMonth.AddMonths(1);
        await LoadMonthAsync();
    }

    [RelayCommand]
    private async Task GoToTodayAsync()
    {
        CurrentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        await LoadMonthAsync();

        // Auto-select today
        var today = Days.FirstOrDefault(d => d.IsToday);
        if (today != null) SelectDay(today);
    }

    [RelayCommand]
    private void SelectDay(CalendarDayViewModel day)
    {
        // Deselect old
        if (SelectedDay is { } old) old.IsSelected = false;

        SelectedDay = day;
        day.IsSelected = true;

        SelectedDayLabel = day.Date.ToString("dddd, d MMMM yyyy");
        HasSelectedDay   = true;

        SelectedDayTasks.Clear();
        foreach (var t in day.Tasks.OrderBy(t => t.Priority).ThenBy(t => t.CreatedAt))
            SelectedDayTasks.Add(t);
    }

    // ── Data loading ─────────────────────────────────────────────────────────

    private async Task RefreshCurrentMonthAsync()
    {
        var prevSelected = SelectedDay?.Date;
        await LoadMonthAsync();
        if (prevSelected.HasValue)
        {
            var match = Days.FirstOrDefault(d => d.Date.Date == prevSelected.Value.Date);
            if (match != null) SelectDay(match);
        }
    }

    private async Task LoadMonthAsync()
    {
        IsBusy = true;
        try
        {
            MonthYearLabel = CurrentMonth.ToString("MMMM yyyy");

            var allTasks = await _todos.GetAllAsync();

            // Group tasks by their deadline date
            var tasksByDate = allTasks
                .Where(t => t.Deadline.HasValue)
                .GroupBy(t => t.Deadline!.Value.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            Days.Clear();

            // Calculate the 42-cell grid (Mon-first week)
            var firstOfMonth = CurrentMonth;
            // Find the Monday on or before the 1st of the month
            int dow = (int)firstOfMonth.DayOfWeek;  // Sun=0, Mon=1, ..., Sat=6
            int offset = dow == 0 ? 6 : dow - 1;    // shift so Monday = col 0
            var gridStart = firstOfMonth.AddDays(-offset);

            for (int i = 0; i < 42; i++)
            {
                var date = gridStart.AddDays(i);
                bool isCurrentMonth = date.Month == CurrentMonth.Month;
                tasksByDate.TryGetValue(date.Date, out var dayTasks);
                Days.Add(new CalendarDayViewModel(date, isCurrentMonth, dayTasks));
            }

            // If today is in the grid, auto-select it; otherwise clear selection
            var today = Days.FirstOrDefault(d => d.IsToday);
            if (today != null && SelectedDay == null)
                SelectDay(today);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
