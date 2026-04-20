using TodoApp.Core.Enums;
using TodoApp.Core.Models;

namespace TodoApp.Services;

public sealed class StatisticsService : IStatisticsService
{
    private readonly ITodoService _todos;

    public StatisticsService(ITodoService todos)
    {
        _todos = todos;
    }

    public async Task<StatisticsSummary> GetSummaryAsync()
    {
        var all   = await _todos.GetAllAsync();
        var today = DateTime.Today;
        var soon  = today.AddDays(3);

        int totalActive    = all.Count(t => t.Status is TodoStatus.Todo or TodoStatus.InProgress);
        int completedToday = all.Count(t => t.CompletedAt?.Date == today);
        int overdue        = all.Count(t => t.IsOverdue);
        int dueSoon        = all.Count(t => t.Deadline.HasValue
                                         && t.Deadline.Value.Date > today
                                         && t.Deadline.Value.Date <= soon
                                         && t.Status != TodoStatus.Done
                                         && t.Status != TodoStatus.Archived);

        var (current, longest) = CalculateStreaks(all);
        double rate = CompletionRate(all, 7);
        var avgTime = AverageCompletionTime(all);

        return new StatisticsSummary(totalActive, completedToday, overdue, dueSoon,
                                     current, longest, rate, avgTime);
    }

    public async Task<List<DailyStats>> GetDailyStatsAsync(int days = 30)
    {
        var all    = await _todos.GetAllAsync();
        var result = new List<DailyStats>(days);
        var today  = DateOnly.FromDateTime(DateTime.Today);

        for (int i = days - 1; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var dt   = date.ToDateTime(TimeOnly.MinValue);

            int created   = all.Count(t => DateOnly.FromDateTime(t.CreatedAt) == date);
            int completed = all.Count(t => t.CompletedAt.HasValue
                                        && DateOnly.FromDateTime(t.CompletedAt.Value) == date);

            result.Add(new DailyStats(date, created, completed));
        }
        return result;
    }

    public async Task<List<PriorityBreakdown>> GetPriorityBreakdownAsync()
    {
        var all = await _todos.GetAllAsync();
        return Enum.GetValues<Priority>()
                   .Select(p => new PriorityBreakdown(
                       p,
                       all.Count(t => t.Priority == p),
                       all.Count(t => t.Priority == p && t.Status == TodoStatus.Done)))
                   .ToList();
    }

    public async Task<Dictionary<string, int>> GetTagFrequencyAsync()
    {
        var all  = await _todos.GetAllAsync();
        return all.SelectMany(t => t.Tags)
                  .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                  .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
    }

    public async Task<int[]> GetCompletionByHourAsync()
    {
        var all    = await _todos.GetAllAsync();
        var result = new int[24];
        foreach (var t in all.Where(t => t.CompletedAt.HasValue))
            result[t.CompletedAt!.Value.Hour]++;
        return result;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (int current, int longest) CalculateStreaks(List<TodoItem> all)
    {
        var completionDays = all
            .Where(t => t.CompletedAt.HasValue)
            .Select(t => t.CompletedAt!.Value.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        if (completionDays.Count == 0) return (0, 0);

        int current = 0;
        var check   = DateTime.Today;

        foreach (var d in completionDays)
        {
            if (d == check || d == check.AddDays(-1))
            {
                current++;
                check = d;
            }
            else break;
        }

        // Longest streak (full scan)
        var sortedAsc = completionDays.OrderBy(d => d).ToList();
        int longest = 1, run = 1;
        for (int i = 1; i < sortedAsc.Count; i++)
        {
            if (sortedAsc[i] == sortedAsc[i - 1].AddDays(1)) run++;
            else                                               run = 1;
            if (run > longest) longest = run;
        }

        return (current, longest);
    }

    private static double CompletionRate(List<TodoItem> all, int days)
    {
        var since     = DateTime.Today.AddDays(-days);
        var created   = all.Count(t => t.CreatedAt >= since);
        var completed = all.Count(t => t.CompletedAt.HasValue && t.CompletedAt >= since);
        return created == 0 ? 0 : Math.Min(1.0, (double)completed / created);
    }

    private static TimeSpan AverageCompletionTime(List<TodoItem> all)
    {
        var completed = all
            .Where(t => t.CompletedAt.HasValue)
            .Select(t => t.CompletedAt!.Value - t.CreatedAt)
            .Where(ts => ts.TotalSeconds > 0)
            .ToList();

        if (completed.Count == 0) return TimeSpan.Zero;
        return TimeSpan.FromTicks((long)completed.Average(ts => ts.Ticks));
    }
}
