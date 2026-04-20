using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TodoApp.Core.Enums;
using TodoApp.Core.Models;
using TodoApp.Infrastructure;
using TodoApp.Services;

namespace TodoApp.ViewModels;

// ── SubTask edit wrapper ─────────────────────────────────────────────────────

public partial class SubTaskEditItem : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private bool   _isCompleted;

    public SubTaskEditItem() { }
    public SubTaskEditItem(SubTask st)
    {
        Id           = st.Id;
        Title        = st.Title;
        IsCompleted  = st.IsCompleted;
    }

    public SubTask ToSubTask(int order) => new()
    {
        Id           = Id,
        Title        = Title,
        IsCompleted  = IsCompleted,
        CompletedAt  = IsCompleted ? DateTime.Now : null,
        DisplayOrder = order
    };
}

// ── Main dialog ViewModel ────────────────────────────────────────────────────

public partial class TaskDetailViewModel : ObservableObject
{
    public bool   IsNew       { get; }
    public bool   Confirmed   { get; private set; }

    /// <summary>Window / header title — looks up the current language resource at call time.</summary>
    public string DialogTitle =>
        System.Windows.Application.Current?.TryFindResource(IsNew ? "Task_AddNewTitle" : "Task_EditTitle") as string
        ?? (IsNew ? "Add New Task" : "Edit Task");

    /// <summary>Fired by Save/Cancel — subscriber closes the dialog window.</summary>
    public event Action<bool>? CloseRequested;

    // ── Core fields ───────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSaveEnabled))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _title = "";

    [ObservableProperty] private string      _description = "";
    [ObservableProperty] private Priority    _priority    = Priority.Medium;
    [ObservableProperty] private TodoStatus  _status      = TodoStatus.Todo;
    [ObservableProperty] private DateTime?   _deadlineDate;
    [ObservableProperty] private string      _deadlineTimeText = "";
    [ObservableProperty] private bool        _isPinned;

    // ── Collections ───────────────────────────────────────────────────────────

    public ObservableCollection<string>        Tags     { get; } = new();
    public ObservableCollection<SubTaskEditItem> SubTasks { get; } = new();

    [ObservableProperty] private string _newTagText      = "";
    [ObservableProperty] private string _newSubTaskTitle = "";

    // ── Label picker ──────────────────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<LabelChipViewModel> _labelChips = new();

    // ── Notification flags ────────────────────────────────────────────────────

    [ObservableProperty] private bool _notifyOneDayBefore      = true;
    [ObservableProperty] private bool _notifyOneHourBefore     = true;
    [ObservableProperty] private bool _notifyFiveMinBefore     = true;
    [ObservableProperty] private bool _notifyAtDeadline        = true;
    [ObservableProperty] private bool _notifyOneDayAfter;

    // ── Recurring ─────────────────────────────────────────────────────────────

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ShowRecurringPanel))]
    private bool _isRecurring;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RecurDaily))]
    [NotifyPropertyChangedFor(nameof(RecurWeekly))]
    [NotifyPropertyChangedFor(nameof(RecurMonthly))]
    [NotifyPropertyChangedFor(nameof(RecurCustom))]
    [NotifyPropertyChangedFor(nameof(ShowWeekdayPicker))]
    [NotifyPropertyChangedFor(nameof(ShowMonthlyPicker))]
    [NotifyPropertyChangedFor(nameof(ShowCustomPicker))]
    private RecurrenceType _recurrenceType = RecurrenceType.Daily;

    [ObservableProperty] private int       _recurrenceInterval   = 1;
    [ObservableProperty] private int       _recurrenceDayOfMonth = 1;
    [ObservableProperty] private DateTime? _recurrenceEndDate;

    // Weekday toggles (for weekly recurrence)
    [ObservableProperty] private bool _repeatMon;
    [ObservableProperty] private bool _repeatTue;
    [ObservableProperty] private bool _repeatWed;
    [ObservableProperty] private bool _repeatThu;
    [ObservableProperty] private bool _repeatFri;
    [ObservableProperty] private bool _repeatSat;
    [ObservableProperty] private bool _repeatSun;

    // Computed visibility helpers
    public bool ShowRecurringPanel => IsRecurring;
    public bool RecurDaily         { get => RecurrenceType == RecurrenceType.Daily;   set { if (value) RecurrenceType = RecurrenceType.Daily;   } }
    public bool RecurWeekly        { get => RecurrenceType == RecurrenceType.Weekly;  set { if (value) RecurrenceType = RecurrenceType.Weekly;  } }
    public bool RecurMonthly       { get => RecurrenceType == RecurrenceType.Monthly; set { if (value) RecurrenceType = RecurrenceType.Monthly; } }
    public bool RecurCustom        { get => RecurrenceType == RecurrenceType.Custom;  set { if (value) RecurrenceType = RecurrenceType.Custom;  } }
    public bool ShowWeekdayPicker  => RecurrenceType == RecurrenceType.Weekly;
    public bool ShowMonthlyPicker  => RecurrenceType == RecurrenceType.Monthly;
    public bool ShowCustomPicker   => RecurrenceType == RecurrenceType.Custom;

    // ── Priority helpers for radio binding ────────────────────────────────────

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

    // ── Status helpers ────────────────────────────────────────────────────────

    public bool StatusTodo       { get => Status == TodoStatus.Todo;       set { if (value) Status = TodoStatus.Todo;       } }
    public bool StatusInProgress { get => Status == TodoStatus.InProgress; set { if (value) Status = TodoStatus.InProgress; } }
    public bool StatusDone       { get => Status == TodoStatus.Done;       set { if (value) Status = TodoStatus.Done;       } }

    partial void OnStatusChanged(TodoStatus value)
    {
        OnPropertyChanged(nameof(StatusTodo));
        OnPropertyChanged(nameof(StatusInProgress));
        OnPropertyChanged(nameof(StatusDone));
    }

    // ── Validation ────────────────────────────────────────────────────────────

    public bool IsSaveEnabled => !string.IsNullOrWhiteSpace(Title);

    // ── Constructors ──────────────────────────────────────────────────────────

    public TaskDetailViewModel()           { IsNew = true; }
    public TaskDetailViewModel(TodoItem t) { IsNew = false; LoadFrom(t); }

    private void LoadFrom(TodoItem t)
    {
        Title       = t.Title;
        Description = t.Description;
        Priority    = t.Priority;
        Status      = t.Status;
        IsPinned    = t.IsPinned;

        if (t.Deadline.HasValue)
        {
            DeadlineDate     = t.Deadline.Value.Date;
            DeadlineTimeText = t.Deadline.Value.ToString("HH:mm");
        }

        foreach (var tag in t.Tags)           Tags.Add(tag);
        foreach (var sub in t.SubTasks.OrderBy(s => s.DisplayOrder))
            SubTasks.Add(new SubTaskEditItem(sub));

        NotifyOneDayBefore  = t.NotificationTimings.HasFlag(NotificationTiming.OneDayBefore);
        NotifyOneHourBefore = t.NotificationTimings.HasFlag(NotificationTiming.OneHourBefore);
        NotifyFiveMinBefore = t.NotificationTimings.HasFlag(NotificationTiming.FiveMinutesBefore);
        NotifyAtDeadline    = t.NotificationTimings.HasFlag(NotificationTiming.AtDeadline);
        NotifyOneDayAfter   = t.NotificationTimings.HasFlag(NotificationTiming.OneDayAfter);

        // Recurring
        IsRecurring = t.IsRecurring;
        if (t.RecurringRule is { } rule)
        {
            RecurrenceType       = rule.Type;
            RecurrenceInterval   = rule.IntervalDays > 0 ? rule.IntervalDays : 1;
            RecurrenceDayOfMonth = rule.DayOfMonth > 0 ? rule.DayOfMonth : 1;
            RecurrenceEndDate    = rule.EndDate;
            RepeatMon = rule.WeekDays.Contains(DayOfWeek.Monday);
            RepeatTue = rule.WeekDays.Contains(DayOfWeek.Tuesday);
            RepeatWed = rule.WeekDays.Contains(DayOfWeek.Wednesday);
            RepeatThu = rule.WeekDays.Contains(DayOfWeek.Thursday);
            RepeatFri = rule.WeekDays.Contains(DayOfWeek.Friday);
            RepeatSat = rule.WeekDays.Contains(DayOfWeek.Saturday);
            RepeatSun = rule.WeekDays.Contains(DayOfWeek.Sunday);
        }
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(IsSaveEnabled))]
    private void Save()
    {
        Confirmed = true;
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    [RelayCommand]
    private void AddTag()
    {
        var t = NewTagText.Trim().Trim('#');
        if (string.IsNullOrEmpty(t) || Tags.Contains(t, StringComparer.OrdinalIgnoreCase)) return;
        Tags.Add(t);
        NewTagText = "";
    }

    [RelayCommand]
    private void RemoveTag(string tag) => Tags.Remove(tag);

    [RelayCommand]
    private void ToggleLabel(LabelChipViewModel chip)
    {
        chip.IsSelected = !chip.IsSelected;
        if (chip.IsSelected)
        {
            if (!Tags.Contains(chip.Label.Name, StringComparer.OrdinalIgnoreCase))
                Tags.Add(chip.Label.Name);
        }
        else
        {
            var existing = Tags.FirstOrDefault(t =>
                t.Equals(chip.Label.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is not null) Tags.Remove(existing);
        }
    }

    /// <summary>
    /// Loads available labels from <see cref="ILabelService"/> via ServiceLocator
    /// and populates <see cref="LabelChips"/> with pre-checked state.
    /// Call once from the dialog constructor (fire-and-forget).
    /// </summary>
    public async Task LoadLabelsAsync()
    {
        var svc = ServiceLocator.TryGet<ILabelService>();
        if (svc is null) return;

        var labels   = await svc.GetAllAsync();
        var selected = new HashSet<string>(Tags, StringComparer.OrdinalIgnoreCase);
        LabelChips   = new ObservableCollection<LabelChipViewModel>(
            labels.Select(l => new LabelChipViewModel(l, selected.Contains(l.Name))));
    }

    [RelayCommand]
    private void AddSubTask()
    {
        if (string.IsNullOrWhiteSpace(NewSubTaskTitle)) return;
        SubTasks.Add(new SubTaskEditItem { Title = NewSubTaskTitle.Trim() });
        NewSubTaskTitle = "";
    }

    [RelayCommand]
    private void RemoveSubTask(SubTaskEditItem sub) => SubTasks.Remove(sub);

    // ── Build result ──────────────────────────────────────────────────────────

    public TodoItem BuildNewItem()
    {
        var item = new TodoItem
        {
            Title       = Title.Trim(),
            Description = Description.Trim(),
            Priority    = Priority,
            Status      = Status,
            IsPinned    = IsPinned,
            IsRecurring = IsRecurring,
            Deadline    = BuildDeadline(),
            Tags        = Tags.ToList(),
            NotificationTimings = BuildTimings(),
            RecurringRule = IsRecurring ? BuildRecurringRule() : null
        };
        for (int i = 0; i < SubTasks.Count; i++)
            item.SubTasks.Add(SubTasks[i].ToSubTask(i));
        return item;
    }

    public void ApplyTo(TodoItem existing)
    {
        existing.Title       = Title.Trim();
        existing.Description = Description.Trim();
        existing.Priority    = Priority;
        existing.Status      = Status;
        existing.IsPinned    = IsPinned;
        existing.IsRecurring = IsRecurring;
        existing.Deadline    = BuildDeadline();
        existing.Tags        = Tags.ToList();
        existing.NotificationTimings = BuildTimings();
        existing.RecurringRule = IsRecurring ? BuildRecurringRule() : null;
        existing.SubTasks.Clear();
        for (int i = 0; i < SubTasks.Count; i++)
            existing.SubTasks.Add(SubTasks[i].ToSubTask(i));
    }

    private DateTime? BuildDeadline()
    {
        if (!DeadlineDate.HasValue) return null;
        if (TimeSpan.TryParse(DeadlineTimeText, out var ts))
            return DeadlineDate.Value.Date.Add(ts);
        return DeadlineDate.Value.Date;
    }

    private NotificationTiming BuildTimings()
    {
        var t = NotificationTiming.None;
        if (NotifyOneDayBefore)  t |= NotificationTiming.OneDayBefore;
        if (NotifyOneHourBefore) t |= NotificationTiming.OneHourBefore;
        if (NotifyFiveMinBefore) t |= NotificationTiming.FiveMinutesBefore;
        if (NotifyAtDeadline)    t |= NotificationTiming.AtDeadline;
        if (NotifyOneDayAfter)   t |= NotificationTiming.OneDayAfter;
        return t;
    }

    private RecurringRule BuildRecurringRule()
    {
        var weekDays = new List<DayOfWeek>();
        if (RepeatMon) weekDays.Add(DayOfWeek.Monday);
        if (RepeatTue) weekDays.Add(DayOfWeek.Tuesday);
        if (RepeatWed) weekDays.Add(DayOfWeek.Wednesday);
        if (RepeatThu) weekDays.Add(DayOfWeek.Thursday);
        if (RepeatFri) weekDays.Add(DayOfWeek.Friday);
        if (RepeatSat) weekDays.Add(DayOfWeek.Saturday);
        if (RepeatSun) weekDays.Add(DayOfWeek.Sunday);

        return new RecurringRule
        {
            Type         = RecurrenceType,
            IntervalDays = RecurrenceInterval > 0 ? RecurrenceInterval : 1,
            DayOfMonth   = RecurrenceDayOfMonth is >= 1 and <= 31 ? RecurrenceDayOfMonth : 1,
            EndDate      = RecurrenceEndDate,
            WeekDays     = weekDays
        };
    }
}
