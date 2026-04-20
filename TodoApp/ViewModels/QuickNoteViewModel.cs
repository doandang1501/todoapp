using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TodoApp.Core.Enums;
using TodoApp.Core.Models;
using TodoApp.Services;

namespace TodoApp.ViewModels;

public partial class QuickNoteViewModel : ObservableObject
{
    private readonly ITodoService       _todos;
    private readonly IWatchLaterService _watchLater;
    private readonly ILabelService      _labelService;

    // ── Bindable properties ───────────────────────────────────────────────────

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _noteText = string.Empty;

    [ObservableProperty] private string _titleText = string.Empty;

    [ObservableProperty] private bool _prioLow      = false;
    [ObservableProperty] private bool _prioMedium   = true;
    [ObservableProperty] private bool _prioHigh     = false;
    [ObservableProperty] private bool _prioCritical = false;

    [ObservableProperty] private bool _isWatchLater = false;

    [ObservableProperty]
    private ObservableCollection<LabelChipViewModel> _labelChips = new();

    public bool CanSave => !string.IsNullOrWhiteSpace(NoteText);

    // ── Close event ───────────────────────────────────────────────────────────

    public event Action<bool>? CloseRequested;

    // ── Constructor ───────────────────────────────────────────────────────────

    public QuickNoteViewModel(
        ITodoService todos,
        IWatchLaterService watchLater,
        ILabelService labelService)
    {
        _todos        = todos;
        _watchLater   = watchLater;
        _labelService = labelService;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!CanSave) return;

        var tags = LabelChips
            .Where(c => c.IsSelected)
            .Select(c => c.Label.Name)
            .ToList();

        if (IsWatchLater)
        {
            try { await _watchLater.CreateAsync(TitleText, url: string.Empty, notes: NoteText, tags: tags); }
            catch { return; }
        }
        else
        {
            var priority = PrioCritical ? Priority.Critical
                         : PrioHigh    ? Priority.High
                         : PrioLow     ? Priority.Low
                         : Priority.Medium;

            var title = string.IsNullOrWhiteSpace(TitleText)
                ? (NoteText.Length > 60 ? NoteText[..60] + "…" : NoteText)
                : TitleText;

            var item = new TodoItem
            {
                Title       = title,
                Description = NoteText,
                Priority    = priority,
                Tags        = tags,
                CreatedAt   = DateTime.Now,
            };
            try { await _todos.CreateAsync(item); }
            catch { return; }
        }

        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    [RelayCommand]
    private void ToggleLabel(LabelChipViewModel chip)
        => chip.IsSelected = !chip.IsSelected;

    // ── Reset for reuse ───────────────────────────────────────────────────────

    public void Reset()
    {
        NoteText     = string.Empty;
        TitleText    = string.Empty;
        PrioLow      = false;
        PrioMedium   = true;
        PrioHigh     = false;
        PrioCritical = false;
        IsWatchLater = false;

        // Reload labels fresh, clearing any prior selections
        _ = LoadLabelChipsAsync();
    }

    private async Task LoadLabelChipsAsync()
    {
        try
        {
            var labels = await _labelService.GetAllAsync();
            LabelChips = new ObservableCollection<LabelChipViewModel>(
                labels.Select(l => new LabelChipViewModel(l)));
        }
        catch { LabelChips = new ObservableCollection<LabelChipViewModel>(); }
    }
}
