using TodoApp.Core.Models;

namespace TodoApp.Services;

public interface IGoalService
{
    Task<List<Goal>> GetAllAsync();
    Task             SaveAsync(List<Goal> goals);
}
