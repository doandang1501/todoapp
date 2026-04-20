using TodoApp.Core.Models;
using TodoApp.Data;

namespace TodoApp.Services;

public sealed class LabelService : ILabelService
{
    private readonly IAppDataStore _store;

    public event EventHandler? LabelsChanged;

    public LabelService(IAppDataStore store) => _store = store;

    public Task<List<Label>> GetAllAsync() => _store.GetLabelsAsync();

    public async Task SaveAsync(List<Label> labels)
    {
        await _store.SaveLabelsAsync(labels);
        LabelsChanged?.Invoke(this, EventArgs.Empty);
    }
}
