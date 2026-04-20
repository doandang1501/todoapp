using TodoApp.Core.Models;
using TodoApp.Data;

namespace TodoApp.Services;

public sealed class GoalService : IGoalService
{
    private readonly IAppDataStore _store;

    public GoalService(IAppDataStore store) => _store = store;

    public Task<List<Goal>> GetAllAsync() => _store.GetGoalsAsync();
    public Task             SaveAsync(List<Goal> goals) => _store.SaveGoalsAsync(goals);
}
