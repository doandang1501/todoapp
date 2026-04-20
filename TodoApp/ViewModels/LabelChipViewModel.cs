using CommunityToolkit.Mvvm.ComponentModel;
using TodoApp.Core.Models;

namespace TodoApp.ViewModels;

/// <summary>
/// Lightweight wrapper used in every label-picker dropdown.
/// Tracks whether this label is currently selected in the host form.
/// </summary>
public partial class LabelChipViewModel : ObservableObject
{
    public Label Label { get; }

    [ObservableProperty]
    private bool _isSelected;

    public LabelChipViewModel(Label label, bool isSelected = false)
    {
        Label      = label;
        _isSelected = isSelected;
    }
}
