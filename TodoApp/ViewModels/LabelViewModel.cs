using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TodoApp.Core.Models;
using TodoApp.Services;
using TodoApp.ViewModels.Base;

namespace TodoApp.ViewModels;

public partial class LabelViewModel : ViewModelBase
{
    private readonly ILabelService           _service;
    private readonly ILogger<LabelViewModel> _logger;

    [ObservableProperty] private ObservableCollection<Label> _labels = new();
    [ObservableProperty] private int    _labelCount;
    [ObservableProperty] private string _newLabelName  = string.Empty;
    [ObservableProperty] private string _newLabelColor = "#E91E63";

    /// <summary>16 preset colours shown as swatches in the add-form palette.</summary>
    public static readonly string[] PresetColors =
    [
        "#E91E63", "#F44336", "#FF5722", "#FF9800",
        "#FFC107", "#8BC34A", "#4CAF50", "#00BCD4",
        "#03A9F4", "#2196F3", "#673AB7", "#9C27B0",
        "#795548", "#607D8B", "#455A64", "#37474F",
    ];

    // ── Constructor ───────────────────────────────────────────────────────────

    public LabelViewModel(ILabelService service, ILogger<LabelViewModel> logger)
    {
        _service = service;
        _logger  = logger;
        _service.LabelsChanged += async (_, _) => await LoadAsync();
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override async Task InitializeAsync()
        => await RunBusyAsync(LoadAsync, "Đang tải…");

    private async Task LoadAsync()
    {
        var all   = await _service.GetAllAsync();
        Labels     = new ObservableCollection<Label>(all);
        LabelCount = Labels.Count;
        _logger.LogDebug("Loaded {Count} labels.", LabelCount);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task AddLabelAsync()
    {
        var name = NewLabelName.Trim();
        if (string.IsNullOrEmpty(name)) return;

        var all = await _service.GetAllAsync();
        if (all.Any(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return; // duplicate

        all.Add(new Label { Name = name, Color = NewLabelColor });
        await _service.SaveAsync(all);
        NewLabelName = string.Empty;
        _logger.LogInformation("Label added: {Name} ({Color})", name, NewLabelColor);
    }

    [RelayCommand]
    private async Task DeleteLabelAsync(Label label)
    {
        var all = await _service.GetAllAsync();
        all.RemoveAll(l => l.Id == label.Id);
        await _service.SaveAsync(all);
        _logger.LogInformation("Label deleted: {Name}", label.Name);
    }

    [RelayCommand]
    private void SelectColor(string color) => NewLabelColor = color;
}
