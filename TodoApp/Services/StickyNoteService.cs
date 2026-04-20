using TodoApp.Core.Models;
using TodoApp.Data;

namespace TodoApp.Services;

public sealed class StickyNoteService : IStickyNoteService
{
    private readonly IAppDataStore _store;

    public event EventHandler? NotesChanged;

    public StickyNoteService(IAppDataStore store)
    {
        _store = store;
    }

    public async Task<List<StickyNote>> GetAllAsync()
        => await _store.GetStickyNotesAsync();

    public async Task<StickyNote> CreateAsync()
    {
        var notes = await _store.GetStickyNotesAsync();
        var note  = new StickyNote
        {
            Left = 200 + notes.Count * 30,
            Top  = 200 + notes.Count * 30
        };
        notes.Add(note);
        await _store.SaveStickyNotesAsync(notes);
        NotesChanged?.Invoke(this, EventArgs.Empty);
        return note;
    }

    public async Task UpdateAsync(StickyNote note)
    {
        var notes = await _store.GetStickyNotesAsync();
        var idx   = notes.FindIndex(n => n.Id == note.Id);
        if (idx >= 0) notes[idx] = note;
        else notes.Add(note);
        note.UpdatedAt = DateTime.Now;
        await _store.SaveStickyNotesAsync(notes);
    }

    public async Task DeleteAsync(Guid id)
    {
        var notes = await _store.GetStickyNotesAsync();
        notes.RemoveAll(n => n.Id == id);
        await _store.SaveStickyNotesAsync(notes);
        NotesChanged?.Invoke(this, EventArgs.Empty);
    }
}
