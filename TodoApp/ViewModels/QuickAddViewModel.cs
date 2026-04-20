using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TodoApp.Core.Enums;
using TodoApp.Core.Models;
using TodoApp.Services;

namespace TodoApp.ViewModels;

/// <summary>
/// ViewModel for the lightweight Quick Add floating window.
/// Accepts title + priority + optional deadline and persists directly.
/// </summary>
public partial class QuickAddViewModel : ObservableObject
{
    private readonly ITodoService _todos;

    /// <summary>Fired after save or cancel — subscriber closes the window.</summary>
    public event Action? CloseRequested;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _titleText = "";

    [ObservableProperty] private Priority  _priority    = Priority.Medium;
    [ObservableProperty] private DateTime? _deadlineDate;
    [ObservableProperty] private string    _deadlineTime = "";

    // Priority radio helpers
    public bool PrioLow      { get => Priority == Priority.Low;      set { if (value) Priority = Priority.Low;      } }
    public bool PrioMedium   { get => Priority == Priority.Medium;   set { if (value) Priority = Priority.Medium;   } }
    public bool PrioHigh     { get => Priority == Priority.High;     set { if (value) Priority = Priority.High;     } }
    public bool PrioCritical { get => Priority == Priority.Critical; set { if (value) Priority = Priority.Critical; } }

    partial void OnPriorityChanged(Priority value)
    {
        OnPropertyChanged(nameof(PrioLow));
        OnPropertyChanged(nameof(PrioMedium));
        OnPropertyChanged(nameof(PrioHigh));
        OnPropertyChanged(nameof(PrioCritical));
    }

    public bool CanSave => !string.IsNullOrWhiteSpace(TitleText);

    public QuickAddViewModel(ITodoService todos)
    {
        _todos = todos;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        var item = new TodoItem
        {
            Title    = TitleText.Trim(),
            Priority = Priority,
            Status   = TodoStatus.Todo,
            Deadline = BuildDeadline(),
            CreatedAt = DateTime.Now
        };

        await _todos.CreateAsync(item);
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();

    private DateTime? BuildDeadline()
    {
        if (!DeadlineDate.HasValue) return null;
        if (TimeSpan.TryParse(DeadlineTime, out var ts))
            return DeadlineDate.Value.Date.Add(ts);
        return DeadlineDate.Value.Date;
    }
}
