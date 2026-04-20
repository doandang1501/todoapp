using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TodoApp.Core.Models;
using TodoApp.Services;
using TodoApp.ViewModels.Base;

namespace TodoApp.ViewModels;

public partial class WatchLaterViewModel : ViewModelBase
{
    private readonly IWatchLaterService           _service;
    private readonly ILabelService                _labelService;
    private readonly ILogger<WatchLaterViewModel> _logger;

    // ── Item list ─────────────────────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<WatchLaterItem> _items = new();

    [ObservableProperty]
    private int _itemCount;

    // ── Add-form fields ───────────────────────────────────────────────────────

    [ObservableProperty] private bool   _isAddFormVisible;
    [ObservableProperty] private bool   _isSaving;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSaveNew))]
    private string _newTitle = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSaveNew))]
    private string _newUrl = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSaveNew))]
    private string _newNotes = string.Empty;

    [ObservableProperty]
    private ObservableCollection<LabelChipViewModel> _labelChips = new();

    public bool CanSaveNew =>
        !string.IsNullOrWhiteSpace(NewUrl) || !string.IsNullOrWhiteSpace(NewNotes);

    // ── Constructor ───────────────────────────────────────────────────────────

    public WatchLaterViewModel(
        IWatchLaterService service,
        ILabelService labelService,
        ILogger<WatchLaterViewModel> logger)
    {
        _service      = service;
        _labelService = labelService;
        _logger       = logger;

        _service.ItemsChanged += async (_, _) => await LoadAsync();
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override async Task InitializeAsync()
        => await RunBusyAsync(LoadAsync, "Đang tải…");

    private async Task LoadAsync()
    {
        var all   = await _service.GetAllAsync();
        Items     = new ObservableCollection<WatchLaterItem>(
                        all.OrderByDescending(i => i.CreatedAt));
        ItemCount = Items.Count;
        _logger.LogDebug("Loaded {Count} watch-later items.", ItemCount);
    }

    // ── Add-form commands ─────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ShowAddFormAsync()
    {
        NewTitle = string.Empty;
        NewUrl   = string.Empty;
        NewNotes = string.Empty;

        var labels = await _labelService.GetAllAsync();
        LabelChips = new ObservableCollection<LabelChipViewModel>(
            labels.Select(l => new LabelChipViewModel(l)));

        IsAddFormVisible = true;
    }

    [RelayCommand]
    private void CancelAdd() => IsAddFormVisible = false;

    [RelayCommand]
    private async Task SaveNewItemAsync()
    {
        if (!CanSaveNew) return;

        IsSaving = true;
        try
        {
            var tags = LabelChips
                .Where(c => c.IsSelected)
                .Select(c => c.Label.Name)
                .ToList();

            await _service.CreateAsync(NewTitle, NewUrl, NewNotes, tags);
            IsAddFormVisible = false;
            _logger.LogInformation("Watch-later item added.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save watch-later item.");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void ToggleLabel(LabelChipViewModel chip)
        => chip.IsSelected = !chip.IsSelected;

    // ── Item commands ─────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task DeleteItemAsync(WatchLaterItem item)
    {
        await _service.DeleteAsync(item.Id);
        _logger.LogInformation("Watch-later item deleted: {Id}", item.Id);
    }

    [RelayCommand]
    private static void OpenUrl(WatchLaterItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.Url))
        {
            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(item.Url)
                    { UseShellExecute = true });
            }
            catch { /* ignore */ }
        }
    }
}
