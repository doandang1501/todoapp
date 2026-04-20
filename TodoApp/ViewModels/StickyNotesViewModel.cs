using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TodoApp.Core.Models;
using TodoApp.Services;
using TodoApp.ViewModels.Base;

namespace TodoApp.ViewModels;

public partial class StickyNotesViewModel : ViewModelBase
{
    private readonly IStickyNoteService           _service;
    private readonly ILogger<StickyNotesViewModel> _logger;

    [ObservableProperty] private ObservableCollection<StickyNote> _notes = new();
    [ObservableProperty] private int _noteCount;

    /// <summary>Raised when the UI should open a StickyNoteWindow for the given note.</summary>
    public event Action<StickyNote>? OpenNoteRequested;

    public StickyNotesViewModel(IStickyNoteService service, ILogger<StickyNotesViewModel> logger)
    {
        _service = service;
        _logger  = logger;

        _service.NotesChanged += async (_, _) =>
        {
            await LoadAsync();
        };
    }

    public override async Task InitializeAsync()
        => await RunBusyAsync(LoadAsync, "Loading notes…");

    private async Task LoadAsync()
    {
        var all = await _service.GetAllAsync();
        Notes     = new ObservableCollection<StickyNote>(all.OrderByDescending(n => n.UpdatedAt ?? n.CreatedAt));
        NoteCount = Notes.Count;
        _logger.LogDebug("Loaded {Count} sticky notes.", NoteCount);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task CreateNoteAsync()
    {
        var note = await _service.CreateAsync();
        OpenNoteRequested?.Invoke(note);
        await LoadAsync();
    }

    [RelayCommand]
    private void OpenNote(StickyNote note)
        => OpenNoteRequested?.Invoke(note);

    [RelayCommand]
    private async Task DeleteNoteAsync(StickyNote note)
    {
        await _service.DeleteAsync(note.Id);
        // NotesChanged will trigger LoadAsync
    }
}
