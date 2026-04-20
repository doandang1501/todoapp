using TodoApp.Core.Enums;

namespace TodoApp.Services;

public record DailyStats(DateOnly Date, int Created, int Completed);

public record PriorityBreakdown(Priority Priority, int Total, int Completed);

public record StatisticsSummary(
    int     TotalActive,
    int     CompletedToday,
    int     Overdue,
    int     DueSoon,           // due in next 3 days
    int     CurrentStreak,     // consecutive days with ≥1 completion
    int     LongestStreak,
    double  CompletionRate7d,  // 0-1
    TimeSpan AverageCompletionTime);

public interface IStatisticsService
{
    Task<StatisticsSummary>        GetSummaryAsync();
    Task<List<DailyStats>>         GetDailyStatsAsync(int days = 30);
    Task<List<PriorityBreakdown>>  GetPriorityBreakdownAsync();

    /// <summary>All unique tags with occurrence counts.</summary>
    Task<Dictionary<string, int>>  GetTagFrequencyAsync();

    /// <summary>Completion count per hour-of-day (index 0-23).</summary>
    Task<int[]>                    GetCompletionByHourAsync();
}
