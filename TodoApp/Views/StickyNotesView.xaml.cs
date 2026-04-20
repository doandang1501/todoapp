using System.Windows.Controls;
using TodoApp.Core.Models;
using TodoApp.Services;
using TodoApp.ViewModels;

namespace TodoApp.Views;

public partial class StickyNotesView : UserControl
{
    private readonly IStickyNoteService _noteService;

    // Track open windows so we don't open the same note twice
    private readonly Dictionary<Guid, StickyNoteWindow> _openWindows = new();

    public StickyNotesView()
    {
        InitializeComponent();

        // Resolve service once from DI — safe because ServiceLocator is set before any view renders
        _noteService = Infrastructure.ServiceLocator.Get<IStickyNoteService>();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender,
        System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is StickyNotesViewModel oldVm)
            oldVm.OpenNoteRequested -= OpenOrFocusNote;

        if (e.NewValue is StickyNotesViewModel newVm)
            newVm.OpenNoteRequested += OpenOrFocusNote;
    }

    private void OpenOrFocusNote(StickyNote note)
    {
        // If already open, bring it to the front
        if (_openWindows.TryGetValue(note.Id, out var existing) && existing.IsLoaded)
        {
            existing.Activate();
            existing.Focus();
            return;
        }

        var window = new StickyNoteWindow(note, _noteService);
        _openWindows[note.Id] = window;
        window.Closed += (_, _) => _openWindows.Remove(note.Id);
        window.Show();
    }
}
