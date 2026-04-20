using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using TodoApp.Core.Enums;
using TodoApp.Services;
using TodoApp.ViewModels.Base;

namespace TodoApp.ViewModels;

public partial class StatisticsViewModel : ViewModelBase
{
    private readonly IStatisticsService _stats;
    private readonly LocalizationService _loc;

    // ── Summary cards ─────────────────────────────────────────────────────────

    [ObservableProperty] private int    _totalActive;
    [ObservableProperty] private int    _completedToday;
    [ObservableProperty] private int    _overdueCount;
    [ObservableProperty] private int    _dueSoonCount;
    [ObservableProperty] private int    _currentStreak;
    [ObservableProperty] private int    _longestStreak;
    [ObservableProperty] private string _completionRate  = "—";
    [ObservableProperty] private string _avgCompletionTime = "—";

    // ── Charts ────────────────────────────────────────────────────────────────

    /// <summary>30-day completions bar chart.</summary>
    [ObservableProperty] private PlotModel? _dailyChart;

    /// <summary>Priority pie/donut breakdown.</summary>
    [ObservableProperty] private PlotModel? _priorityChart;

    /// <summary>Completion by hour-of-day bar chart.</summary>
    [ObservableProperty] private PlotModel? _hourlyChart;

    // ── Tag leaderboard ───────────────────────────────────────────────────────

    public ObservableCollection<(string Tag, int Count)> TopTags { get; } = new();

    // ── Constructor ───────────────────────────────────────────────────────────

    public StatisticsViewModel(IStatisticsService stats, LocalizationService loc)
    {
        _stats = stats;
        _loc   = loc;
    }

    // ── Init & refresh ────────────────────────────────────────────────────────

    public override async Task InitializeAsync()
        => await RunBusyAsync(LoadAllAsync, "Loading statistics…");

    [RelayCommand]
    private async Task RefreshAsync()
        => await RunBusyAsync(LoadAllAsync, "Refreshing…");

    private async Task LoadAllAsync()
    {
        var summaryTask   = _stats.GetSummaryAsync();
        var dailyTask     = _stats.GetDailyStatsAsync(30);
        var priorityTask  = _stats.GetPriorityBreakdownAsync();
        var tagTask       = _stats.GetTagFrequencyAsync();
        var hourlyTask    = _stats.GetCompletionByHourAsync();

        await Task.WhenAll(summaryTask, dailyTask, priorityTask, tagTask, hourlyTask);

        var summary  = summaryTask.Result;
        var daily    = dailyTask.Result;
        var priority = priorityTask.Result;
        var tags     = tagTask.Result;
        var hourly   = hourlyTask.Result;

        // Summary cards
        TotalActive       = summary.TotalActive;
        CompletedToday    = summary.CompletedToday;
        OverdueCount      = summary.Overdue;
        DueSoonCount      = summary.DueSoon;
        CurrentStreak     = summary.CurrentStreak;
        LongestStreak     = summary.LongestStreak;
        CompletionRate    = $"{summary.CompletionRate7d:P0}";
        AvgCompletionTime = FormatTimeSpan(summary.AverageCompletionTime);

        // Charts
        DailyChart    = BuildDailyChart(daily,    _loc.Translate("Chart_Daily30Days"));
        PriorityChart = BuildPriorityChart(priority, _loc.Translate("Chart_ByPriority"));
        HourlyChart   = BuildHourlyChart(hourly,  _loc.Translate("Chart_ByHour"));

        // Tags
        TopTags.Clear();
        foreach (var kv in tags.OrderByDescending(kv => kv.Value).Take(10))
            TopTags.Add((kv.Key, kv.Value));
    }

    // ── Chart builders ────────────────────────────────────────────────────────

    private static PlotModel BuildDailyChart(List<DailyStats> data, string title)
    {
        var model = new PlotModel
        {
            Title              = title,
            TitleFontSize      = 13,
            TitleColor         = OxyColor.FromRgb(33, 33, 33),
            Background         = OxyColors.Transparent,
            PlotAreaBackground = OxyColors.Transparent,
        };

        // LinearBarSeries needs LinearAxis on X (not CategoryAxis)
        int labelStep = Math.Max(1, data.Count / 6);
        var xAxis = new LinearAxis
        {
            Position           = AxisPosition.Bottom,
            Minimum            = -0.5,
            Maximum            = data.Count - 0.5,
            IsZoomEnabled      = false,
            IsPanEnabled       = false,
            TextColor          = OxyColor.FromRgb(117, 117, 117),
            TicklineColor      = OxyColors.Transparent,
            MajorGridlineColor = OxyColors.Transparent,
            MajorStep          = 1,
            LabelFormatter     = v =>
            {
                int idx = (int)Math.Round(v);
                return idx >= 0 && idx < data.Count && idx % labelStep == 0
                    ? data[idx].Date.ToString("MMM d") : "";
            }
        };

        var yAxis = new LinearAxis
        {
            Position           = AxisPosition.Left,
            Minimum            = 0,
            MinimumPadding     = 0,
            IsZoomEnabled      = false,
            IsPanEnabled       = false,
            TextColor          = OxyColor.FromRgb(117, 117, 117),
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(40, 0, 0, 0),
            TicklineColor      = OxyColors.Transparent,
        };

        var series = new LinearBarSeries
        {
            FillColor       = OxyColor.FromRgb(233, 30, 99),
            StrokeColor     = OxyColors.Transparent,
            StrokeThickness = 0,
            BarWidth        = 0.8,
        };
        for (int i = 0; i < data.Count; i++)
            series.Points.Add(new DataPoint(i, data[i].Completed));

        model.Axes.Add(xAxis);
        model.Axes.Add(yAxis);
        model.Series.Add(series);
        return model;
    }

    private static PlotModel BuildPriorityChart(List<PriorityBreakdown> data, string title)
    {
        var model = new PlotModel
        {
            Title          = title,
            TitleFontSize  = 13,
            TitleColor     = OxyColor.FromRgb(33, 33, 33),
            Background     = OxyColors.Transparent,
            PlotAreaBackground = OxyColors.Transparent,
        };

        var series = new PieSeries
        {
            StrokeThickness    = 2,
            InsideLabelPosition = 0.8,
            AngleSpan          = 360,
            StartAngle         = 90,
            InsideLabelFormat  = "{1}",
            OutsideLabelFormat = "{0}",
            FontSize           = 11,
        };

        var colours = new Dictionary<Priority, OxyColor>
        {
            [Priority.Low]      = OxyColor.FromRgb(76,  175, 80),
            [Priority.Medium]   = OxyColor.FromRgb(255, 152, 0),
            [Priority.High]     = OxyColor.FromRgb(244, 67,  54),
            [Priority.Critical] = OxyColor.FromRgb(156, 39, 176),
        };

        foreach (var b in data.Where(b => b.Total > 0))
        {
            series.Slices.Add(new PieSlice(b.Priority.ToString(), b.Total)
            {
                Fill = colours.TryGetValue(b.Priority, out var c)
                    ? c : OxyColors.Gray,
            });
        }

        model.Series.Add(series);
        return model;
    }

    private static PlotModel BuildHourlyChart(int[] hourly, string title)
    {
        var model = new PlotModel
        {
            Title              = title,
            TitleFontSize      = 13,
            TitleColor         = OxyColor.FromRgb(33, 33, 33),
            Background         = OxyColors.Transparent,
            PlotAreaBackground = OxyColors.Transparent,
        };

        var xAxis = new LinearAxis
        {
            Position           = AxisPosition.Bottom,
            Minimum            = -0.5,
            Maximum            = 23.5,
            IsZoomEnabled      = false,
            IsPanEnabled       = false,
            TextColor          = OxyColor.FromRgb(117, 117, 117),
            TicklineColor      = OxyColors.Transparent,
            MajorGridlineColor = OxyColors.Transparent,
            MajorStep          = 1,
            LabelFormatter     = v =>
            {
                int h = (int)Math.Round(v);
                return h >= 0 && h < 24 && h % 3 == 0 ? $"{h:D2}" : "";
            }
        };

        var yAxis = new LinearAxis
        {
            Position           = AxisPosition.Left,
            Minimum            = 0,
            MinimumPadding     = 0,
            IsZoomEnabled      = false,
            IsPanEnabled       = false,
            TextColor          = OxyColor.FromRgb(117, 117, 117),
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(40, 0, 0, 0),
            TicklineColor      = OxyColors.Transparent,
        };

        var series = new LinearBarSeries
        {
            FillColor       = OxyColor.FromArgb(180, 233, 30, 99),
            StrokeColor     = OxyColors.Transparent,
            StrokeThickness = 0,
            BarWidth        = 0.8,
        };
        for (int h = 0; h < Math.Min(24, hourly.Length); h++)
            series.Points.Add(new DataPoint(h, hourly[h]));

        model.Axes.Add(xAxis);
        model.Axes.Add(yAxis);
        model.Series.Add(series);
        return model;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts == TimeSpan.Zero) return "—";
        if (ts.TotalDays >= 1)   return $"{(int)ts.TotalDays}d {ts.Hours}h";
        if (ts.TotalHours >= 1)  return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        return $"{ts.Minutes}m";
    }
}
