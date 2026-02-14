using ScottPlot;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Application.Models;

namespace TimeSheet.Core.Application.Services;

/// <summary>
/// Service for generating chart images from report data using ScottPlot.
/// </summary>
public class ChartGenerationService : IChartGenerationService
{
    private const int ChartWidth = 800;
    private const int ChartHeight = 600;

    /// <summary>
    /// Generates a bar chart showing work hours by day from a daily breakdown report.
    /// </summary>
    public byte[] GenerateDailyBreakdownChart(List<DailyBreakdownRow> breakdown, string periodLabel)
    {
        // Filter to days with activity
        var activeDays = breakdown.Where(d => d.HasActivity).ToList();

        if (activeDays.Count == 0)
        {
            return GenerateEmptyChart("No data available", periodLabel);
        }

        var plot = new Plot();

        // Prepare data
        var dates = activeDays.Select(d => d.Date.ToString("MM/dd")).ToArray();
        var workHours = activeDays.Select(d => (double)d.WorkHours).ToArray();
        var positions = Enumerable.Range(0, activeDays.Count).Select(i => (double)i).ToArray();

        // Add bar chart
        var barPlot = plot.Add.Bars(positions, workHours);
        barPlot.Color = ScottPlot.Color.FromHex("#4472C4"); // Professional blue

        // Configure axes
        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
            positions,
            dates);
        plot.Axes.Bottom.Label.Text = "Date";
        plot.Axes.Left.Label.Text = "Work Hours";

        // Set title
        plot.Title($"Daily Work Hours - {periodLabel}");

        // Style
        plot.Layout.Frameless();
        plot.Axes.Margins(bottom: 0);

        return RenderPlotToBytes(plot);
    }

    /// <summary>
    /// Generates a line chart showing work hours trend over time.
    /// </summary>
    public byte[] GenerateWorkHoursTrendChart(List<DailyBreakdownRow> breakdown, string periodLabel)
    {
        // Filter to days with activity
        var activeDays = breakdown.Where(d => d.HasActivity).ToList();

        if (activeDays.Count == 0)
        {
            return GenerateEmptyChart("No data available", periodLabel);
        }

        var plot = new Plot();

        // Prepare data
        var dates = activeDays.Select(d => d.Date.ToOADate()).ToArray();
        var workHours = activeDays.Select(d => (double)d.WorkHours).ToArray();

        // Add line plot with markers
        var linePlot = plot.Add.ScatterLine(dates, workHours);
        linePlot.LineWidth = 2;
        linePlot.Color = ScottPlot.Color.FromHex("#4472C4");
        linePlot.MarkerSize = 8;
        linePlot.MarkerShape = MarkerShape.FilledCircle;

        // Configure date axis
        plot.Axes.DateTimeTicksBottom();
        plot.Axes.Bottom.Label.Text = "Date";
        plot.Axes.Left.Label.Text = "Work Hours";

        // Set title
        plot.Title($"Work Hours Trend - {periodLabel}");

        // Style
        plot.Layout.Frameless();

        return RenderPlotToBytes(plot);
    }

    /// <summary>
    /// Generates a grouped bar chart showing the breakdown of activities (work, commute, lunch) by day.
    /// Note: Simplified from stacked to grouped bars due to ScottPlot 5.x API limitations.
    /// </summary>
    public byte[] GenerateActivityBreakdownChart(List<DailyBreakdownRow> breakdown, string periodLabel)
    {
        // Filter to days with activity
        var activeDays = breakdown.Where(d => d.HasActivity).ToList();

        if (activeDays.Count == 0)
        {
            return GenerateEmptyChart("No data available", periodLabel);
        }

        var plot = new Plot();

        // Prepare data
        var dates = activeDays.Select(d => d.Date.ToString("MM/dd")).ToArray();
        var positions = Enumerable.Range(0, activeDays.Count).Select(i => (double)i).ToArray();

        var workHours = activeDays.Select(d => (double)d.WorkHours).ToArray();
        var commuteHours = activeDays.Select(d => (double)(d.CommuteToWorkHours + d.CommuteToHomeHours)).ToArray();
        var lunchHours = activeDays.Select(d => (double)d.LunchHours).ToArray();

        // Use grouped bars instead of stacked (simpler with ScottPlot 5.x)
        var barWidth = 0.25;

        var workBars = plot.Add.Bars(positions.Select(p => p - barWidth).ToArray(), workHours);
        workBars.Color = ScottPlot.Color.FromHex("#4472C4"); // Blue
        workBars.LegendText = "Work";

        var commuteBars = plot.Add.Bars(positions, commuteHours);
        commuteBars.Color = ScottPlot.Color.FromHex("#ED7D31"); // Orange
        commuteBars.LegendText = "Commute";

        var lunchBars = plot.Add.Bars(positions.Select(p => p + barWidth).ToArray(), lunchHours);
        lunchBars.Color = ScottPlot.Color.FromHex("#A5A5A5"); // Gray
        lunchBars.LegendText = "Lunch";

        // Configure axes
        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
            positions,
            dates);
        plot.Axes.Bottom.Label.Text = "Date";
        plot.Axes.Left.Label.Text = "Hours";

        // Set title and legend
        plot.Title($"Activity Breakdown - {periodLabel}");
        plot.ShowLegend(Alignment.UpperRight);

        // Style
        plot.Layout.Frameless();
        plot.Axes.Margins(bottom: 0);

        return RenderPlotToBytes(plot);
    }

    /// <summary>
    /// Generates a horizontal bar chart comparing daily averages across multiple time periods.
    /// </summary>
    public byte[] GenerateDailyAveragesComparisonChart(
        DailyAveragesReport report7Days,
        DailyAveragesReport report30Days,
        DailyAveragesReport report90Days)
    {
        var plot = new Plot();

        // Prepare data - we'll show work, commute, and lunch averages
        var categories = new[] { "7 Days", "30 Days", "90 Days" };
        var positions = new double[] { 0, 1, 2 };

        var workData = new double[]
        {
            (double)report7Days.AverageWorkHours,
            (double)report30Days.AverageWorkHours,
            (double)report90Days.AverageWorkHours
        };

        var commuteData = new double[]
        {
            (double)(report7Days.AverageCommuteToWorkHours + report7Days.AverageCommuteToHomeHours),
            (double)(report30Days.AverageCommuteToWorkHours + report30Days.AverageCommuteToHomeHours),
            (double)(report90Days.AverageCommuteToWorkHours + report90Days.AverageCommuteToHomeHours)
        };

        var lunchData = new double[]
        {
            (double)report7Days.AverageLunchHours,
            (double)report30Days.AverageLunchHours,
            (double)report90Days.AverageLunchHours
        };

        // Add grouped bars
        var barWidth = 0.25;

        var workBars = plot.Add.Bars(positions.Select(p => p - barWidth).ToArray(), workData);
        workBars.Color = ScottPlot.Color.FromHex("#4472C4");
        workBars.LegendText = "Work";

        var commuteBars = plot.Add.Bars(positions, commuteData);
        commuteBars.Color = ScottPlot.Color.FromHex("#ED7D31");
        commuteBars.LegendText = "Commute";

        var lunchBars = plot.Add.Bars(positions.Select(p => p + barWidth).ToArray(), lunchData);
        lunchBars.Color = ScottPlot.Color.FromHex("#A5A5A5");
        lunchBars.LegendText = "Lunch";

        // Configure axes
        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
            positions,
            categories);
        plot.Axes.Bottom.Label.Text = "Period";
        plot.Axes.Left.Label.Text = "Average Hours per Day";

        // Set title and legend
        plot.Title("Daily Averages Comparison");
        plot.ShowLegend(Alignment.UpperRight);

        // Style
        plot.Layout.Frameless();
        plot.Axes.Margins(bottom: 0);

        return RenderPlotToBytes(plot);
    }

    /// <summary>
    /// Generates a grouped bar chart showing commute patterns by day of week.
    /// </summary>
    public byte[] GenerateCommutePatternsChart(
        List<CommutePattern> toWorkPatterns,
        List<CommutePattern> toHomePatterns)
    {
        if (toWorkPatterns.Count == 0 && toHomePatterns.Count == 0)
        {
            return GenerateEmptyChart("No commute data available", "Commute Patterns");
        }

        var plot = new Plot();

        // Merge patterns by day of week
        var allDays = Enum.GetValues<DayOfWeek>()
            .OrderBy(d => d == DayOfWeek.Sunday ? 7 : (int)d) // Monday-Sunday order
            .ToList();

        var toWorkByDay = toWorkPatterns.ToDictionary(p => p.DayOfWeek);
        var toHomeByDay = toHomePatterns.ToDictionary(p => p.DayOfWeek);

        // Filter to days with data
        var daysWithData = allDays
            .Where(d => toWorkByDay.ContainsKey(d) || toHomeByDay.ContainsKey(d))
            .ToList();

        if (daysWithData.Count == 0)
        {
            return GenerateEmptyChart("No commute data available", "Commute Patterns");
        }

        // Prepare data
        var dayNames = daysWithData.Select(d => GetDayName(d)).ToArray();
        var positions = Enumerable.Range(0, daysWithData.Count).Select(i => (double)i).ToArray();

        var toWorkDurations = daysWithData.Select(d =>
            toWorkByDay.TryGetValue(d, out var pattern) ? (double)pattern.AverageDurationHours : 0.0).ToArray();

        var toHomeDurations = daysWithData.Select(d =>
            toHomeByDay.TryGetValue(d, out var pattern) ? (double)pattern.AverageDurationHours : 0.0).ToArray();

        // Add grouped bars
        var barWidth = 0.35;

        var toWorkBars = plot.Add.Bars(positions.Select(p => p - barWidth / 2).ToArray(), toWorkDurations);
        toWorkBars.Color = ScottPlot.Color.FromHex("#4472C4");
        toWorkBars.LegendText = "To Work";

        var toHomeBars = plot.Add.Bars(positions.Select(p => p + barWidth / 2).ToArray(), toHomeDurations);
        toHomeBars.Color = ScottPlot.Color.FromHex("#ED7D31");
        toHomeBars.LegendText = "To Home";

        // Configure axes
        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
            positions,
            dayNames);
        plot.Axes.Bottom.Label.Text = "Day of Week";
        plot.Axes.Left.Label.Text = "Average Duration (hours)";

        // Set title and legend
        plot.Title("Commute Patterns by Day of Week");
        plot.ShowLegend(Alignment.UpperRight);

        // Style
        plot.Layout.Frameless();
        plot.Axes.Margins(bottom: 0);

        return RenderPlotToBytes(plot);
    }

    /// <summary>
    /// Renders a plot to a byte array (PNG format).
    /// </summary>
    private static byte[] RenderPlotToBytes(Plot plot)
    {
        // ScottPlot 5.x GetImageBytes method returns PNG bytes directly
        return plot.GetImageBytes(ChartWidth, ChartHeight);
    }

    /// <summary>
    /// Generates an empty chart with a message.
    /// </summary>
    private static byte[] GenerateEmptyChart(string message, string title)
    {
        var plot = new Plot();
        plot.Title(title);

        // Add text annotation in the center
        var text = plot.Add.Text(message, 0.5, 0.5);
        text.LabelFontSize = 18;
        text.LabelFontColor = ScottPlot.Color.FromHex("#666666");

        plot.Axes.SetLimits(0, 1, 0, 1);
        plot.HideGrid();
        plot.Layout.Frameless();

        return RenderPlotToBytes(plot);
    }

    /// <summary>
    /// Gets the display name for a day of the week.
    /// </summary>
    private static string GetDayName(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => "Mon",
            DayOfWeek.Tuesday => "Tue",
            DayOfWeek.Wednesday => "Wed",
            DayOfWeek.Thursday => "Thu",
            DayOfWeek.Friday => "Fri",
            DayOfWeek.Saturday => "Sat",
            DayOfWeek.Sunday => "Sun",
            _ => day.ToString()
        };
    }
}
