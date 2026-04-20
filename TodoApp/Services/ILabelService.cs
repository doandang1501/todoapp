using TodoApp.Core.Models;

namespace TodoApp.Services;

public interface ILabelService
{
    Task<List<Label>> GetAllAsync();
    Task              SaveAsync(List<Label> labels);
    event EventHandler LabelsChanged;
}
