using CommunityToolkit.Mvvm.ComponentModel;
using TodoApp.Core.Models;

namespace TodoApp.ViewModels;

public partial class GoalItemViewModel : ObservableObject
{
    public Goal Goal { get; }

    [ObservableProperty] private double _progressRatio;
    [ObservableProperty] private string _progressPercent = string.Empty;
    [ObservableProperty] private string _daysText        = string.Empty;
    [ObservableProperty] private bool   _canCheckToday;
    [ObservableProperty] private bool   _isCompleted;

    public GoalItemViewModel(Goal goal)
    {
        Goal = goal;
        Refresh();
    }

    public void Refresh()
    {
        ProgressRatio   = Goal.TotalDays > 0 ? Math.Min(1.0, (double)Goal.CompletedDays / Goal.TotalDays) : 0;
        ProgressPercent = $"{(int)(ProgressRatio * 100)}%";
        DaysText        = $"{Goal.CompletedDays}/{Goal.TotalDays} ngày";
        CanCheckToday   = !Goal.IsCompleted && Goal.LastCheckedDate?.Date != DateTime.Today;
        IsCompleted     = Goal.IsCompleted;
    }
}
