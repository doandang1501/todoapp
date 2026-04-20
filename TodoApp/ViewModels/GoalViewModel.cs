using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TodoApp.Core.Models;
using TodoApp.Services;
using TodoApp.ViewModels.Base;

namespace TodoApp.ViewModels;

public partial class GoalViewModel : ViewModelBase
{
    private readonly IGoalService _goalService;

    [ObservableProperty] private ObservableCollection<GoalItemViewModel> _goals = new();

    // Add form
    [ObservableProperty] private bool   _isAddFormVisible;
    [ObservableProperty] private string _newTitle       = string.Empty;
    [ObservableProperty] private string _newDescription = string.Empty;
    [ObservableProperty] private int    _newTotalDays   = 30;

    // Edit form
    [ObservableProperty] private bool                _isEditFormVisible;
    [ObservableProperty] private GoalItemViewModel?  _editingItem;

    // Celebration modal
    [ObservableProperty] private bool   _showCelebration;
    [ObservableProperty] private string _celebrationGoalTitle = string.Empty;

    public int GoalCount => Goals.Count;

    public GoalViewModel(IGoalService goalService) => _goalService = goalService;

    public override async Task InitializeAsync()
    {
        var goals = await _goalService.GetAllAsync();
        Goals = new ObservableCollection<GoalItemViewModel>(
            goals.Select(g => new GoalItemViewModel(g)));
        Goals.CollectionChanged += (_, _) => OnPropertyChanged(nameof(GoalCount));
        OnPropertyChanged(nameof(GoalCount));
    }

    partial void OnGoalsChanged(ObservableCollection<GoalItemViewModel> value)
    {
        if (value != null)
            value.CollectionChanged += (_, _) => OnPropertyChanged(nameof(GoalCount));
        OnPropertyChanged(nameof(GoalCount));
    }

    // ── Add ───────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ShowAddForm()
    {
        NewTitle = string.Empty;
        NewDescription = string.Empty;
        NewTotalDays = 30;
        IsEditFormVisible = false;
        EditingItem = null;
        IsAddFormVisible = true;
    }

    [RelayCommand]
    private void CancelAdd() => IsAddFormVisible = false;

    [RelayCommand]
    private async Task SaveNewGoalAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTitle)) return;

        var goal = new Goal
        {
            Title       = NewTitle.Trim(),
            Description = NewDescription.Trim(),
            TotalDays   = Math.Max(1, NewTotalDays),
        };

        var all = Goals.Select(g => g.Goal).ToList();
        all.Add(goal);
        await _goalService.SaveAsync(all);
        Goals.Add(new GoalItemViewModel(goal));
        IsAddFormVisible = false;
    }

    // ── Edit ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void StartEdit(GoalItemViewModel item)
    {
        EditingItem = item;
        NewTitle = item.Goal.Title;
        NewDescription = item.Goal.Description;
        NewTotalDays = item.Goal.TotalDays;
        IsAddFormVisible = false;
        IsEditFormVisible = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditFormVisible = false;
        EditingItem = null;
    }

    [RelayCommand]
    private async Task SaveEditAsync()
    {
        if (EditingItem is null || string.IsNullOrWhiteSpace(NewTitle)) return;

        EditingItem.Goal.Title       = NewTitle.Trim();
        EditingItem.Goal.Description = NewDescription.Trim();
        EditingItem.Goal.TotalDays   = Math.Max(EditingItem.Goal.CompletedDays, Math.Max(1, NewTotalDays));
        EditingItem.Refresh();

        await _goalService.SaveAsync(Goals.Select(g => g.Goal).ToList());
        IsEditFormVisible = false;
        EditingItem = null;
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task DeleteGoalAsync(GoalItemViewModel item)
    {
        Goals.Remove(item);
        await _goalService.SaveAsync(Goals.Select(g => g.Goal).ToList());
    }

    // ── Daily tick ────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task CheckTodayAsync(GoalItemViewModel item)
    {
        if (!item.CanCheckToday) return;

        item.Goal.CompletedDays++;
        item.Goal.LastCheckedDate = DateTime.Now;

        bool justCompleted = item.Goal.CompletedDays >= item.Goal.TotalDays;
        if (justCompleted && !item.Goal.IsCompleted)
        {
            item.Goal.IsCompleted = true;
            item.Goal.CompletedAt = DateTime.Now;
        }

        item.Refresh();
        await _goalService.SaveAsync(Goals.Select(g => g.Goal).ToList());

        if (justCompleted)
        {
            CelebrationGoalTitle = item.Goal.Title;
            ShowCelebration = true;
        }
    }

    [RelayCommand]
    private void CloseCelebration() => ShowCelebration = false;
}
