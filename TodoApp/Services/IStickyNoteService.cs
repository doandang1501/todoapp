using TodoApp.Core.Models;

namespace TodoApp.Services;

public interface IStickyNoteService
{
    Task<List<StickyNote>> GetAllAsync();
    Task<StickyNote>       CreateAsync();
    Task                   UpdateAsync(StickyNote note);
    Task                   DeleteAsync(Guid id);
    event EventHandler     NotesChanged;
}
