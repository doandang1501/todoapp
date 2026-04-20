using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TodoApp.Core.Enums;
using TodoApp.Core.Models;

namespace TodoApp.Services;

/// <summary>
/// Background service that wakes up every hour and generates new instances
/// of recurring tasks whose next occurrence is now due.
/// </summary>
public sealed class RecurringTaskService : BackgroundService
{
    private static readonly TimeSpan _interval = TimeSpan.FromHours(1);

    private readonly ITodoService         _todos;
    private readonly ILogger<RecurringTaskService> _logger;

    public RecurringTaskService(ITodoService todos,
                                ILogger<RecurringTaskService> logger)
    {
        _todos  = todos;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give the rest of the app time to start up before first check
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateDueInstancesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error generating recurring task instances.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task GenerateDueInstancesAsync(CancellationToken ct)
    {
        var all = await _todos.GetAllAsync();
        var now = DateTime.Now;

        // Only parent recurring tasks (no RecurringParentId)
        var recurringParents = all
            .Where(t => t.IsRecurring
                     && t.RecurringRule is not null
                     && t.RecurringParentId is null
                     && t.Status != TodoStatus.Archived)
            .ToList();

        foreach (var parent in recurringParents)
        {
            ct.ThrowIfCancellationRequested();
            var rule = parent.RecurringRule!;

            // Check max instances (0 = unlimited)
            if (rule.MaxInstances > 0 && rule.GeneratedCount >= rule.MaxInstances)
                continue;

            // Check end date
            if (rule.EndDate.HasValue && now > rule.EndDate.Value)
                continue;

            var nextDate = CalculateNextDate(parent, rule, all);
            if (nextDate is null || nextDate.Value > now) continue;

            // Create child instance
            var child = parent.Duplicate();
            child.Id                = Guid.NewGuid();
            child.RecurringParentId = parent.Id;
            child.IsRecurring       = false;
            child.RecurringRule     = null;
            child.Status            = TodoStatus.Todo;
            child.CompletedAt       = null;
            child.CreatedAt         = now;
            child.UpdatedAt         = now;
            child.Deadline          = nextDate;
            child.IsPinned          = false;

            await _todos.CreateAsync(child);

            // Update parent's tracking data
            rule.GeneratedCount++;
            rule.LastGeneratedAt = now;
            parent.UpdatedAt     = now;
            await _todos.UpdateAsync(parent);

            _logger.LogInformation(
                "Generated recurring instance for '{Title}' (parent {ParentId}), due {Due:g}",
                child.Title, parent.Id, child.Deadline);
        }
    }

    // ── Next occurrence calculation ──────────────────────────────────────────

    private static DateTime? CalculateNextDate(
        TodoItem       parent,
        RecurringRule  rule,
        List<TodoItem> all)
    {
        // Base: when was the last instance generated (or parent created)?
        var lastGen = rule.LastGeneratedAt ?? parent.CreatedAt;

        return rule.Type switch
        {
            RecurrenceType.Daily   => lastGen.AddDays(1),
            RecurrenceType.Weekly  => CalculateNextWeekly(lastGen, rule),
            RecurrenceType.Monthly => CalculateNextMonthly(lastGen, rule),
            RecurrenceType.Custom  => lastGen.AddDays(rule.IntervalDays > 0 ? rule.IntervalDays : 1),
            _                      => null
        };
    }

    private static DateTime CalculateNextWeekly(DateTime from, RecurringRule rule)
    {
        if (rule.WeekDays.Count == 0) return from.AddDays(7);

        var candidate = from.AddDays(1);
        for (int i = 0; i < 7; i++)
        {
            if (rule.WeekDays.Contains(candidate.DayOfWeek))
                return candidate;
            candidate = candidate.AddDays(1);
        }
        return from.AddDays(7); // fallback
    }

    private static DateTime CalculateNextMonthly(DateTime from, RecurringRule rule)
    {
        var next  = from.AddMonths(1);
        int day   = rule.DayOfMonth > 0 ? rule.DayOfMonth : from.Day;
        int maxDay = DateTime.DaysInMonth(next.Year, next.Month);
        return new DateTime(next.Year, next.Month, Math.Min(day, maxDay),
                            from.Hour, from.Minute, 0);
    }
}
