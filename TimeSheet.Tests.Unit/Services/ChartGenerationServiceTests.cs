using TimeSheet.Core.Application.Models;
using TimeSheet.Core.Application.Services;

namespace TimeSheet.Tests.Unit.Services;

public class ChartGenerationServiceTests
{
    private readonly ChartGenerationService _service;

    public ChartGenerationServiceTests()
    {
        _service = new ChartGenerationService();
    }

    [Fact]
    public void GenerateDailyBreakdownChart_WithValidData_ReturnsNonEmptyPngData()
    {
        // Arrange
        var breakdown = new List<DailyBreakdownRow>
        {
            new() { Date = new DateTime(2026, 2, 3), WorkHours = 8m, HasActivity = true },
            new() { Date = new DateTime(2026, 2, 4), WorkHours = 7.5m, HasActivity = true },
            new() { Date = new DateTime(2026, 2, 5), WorkHours = 8.5m, HasActivity = true }
        };

        // Act
        var result = _service.GenerateDailyBreakdownChart(breakdown, "Test Week");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // PNG files should have a reasonable size (at least a few hundred bytes)
        Assert.True(result.Length > 100, $"Expected image size > 100 bytes, got {result.Length}");
    }

    [Fact]
    public void GenerateDailyBreakdownChart_WithEmptyData_ReturnsEmptyChartPng()
    {
        // Arrange
        var breakdown = new List<DailyBreakdownRow>();

        // Act
        var result = _service.GenerateDailyBreakdownChart(breakdown, "Empty Period");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 1000); // Should still generate a chart with "No data" message
    }

    [Fact]
    public void GenerateWorkHoursTrendChart_WithValidData_ReturnsNonEmptyPngData()
    {
        // Arrange
        var breakdown = new List<DailyBreakdownRow>
        {
            new() { Date = new DateTime(2026, 2, 3), WorkHours = 8m, HasActivity = true },
            new() { Date = new DateTime(2026, 2, 4), WorkHours = 7.5m, HasActivity = true },
            new() { Date = new DateTime(2026, 2, 5), WorkHours = 8.5m, HasActivity = true },
            new() { Date = new DateTime(2026, 2, 6), WorkHours = 9m, HasActivity = true },
            new() { Date = new DateTime(2026, 2, 7), WorkHours = 7m, HasActivity = true }
        };

        // Act
        var result = _service.GenerateWorkHoursTrendChart(breakdown, "Test Week");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // PNG files should have a reasonable size
        Assert.True(result.Length > 100, $"Expected image size > 100 bytes, got {result.Length}");
    }

    [Fact]
    public void GenerateActivityBreakdownChart_WithValidData_ReturnsNonEmptyPngData()
    {
        // Arrange
        var breakdown = new List<DailyBreakdownRow>
        {
            new()
            {
                Date = new DateTime(2026, 2, 3),
                WorkHours = 8m,
                CommuteToWorkHours = 0.5m,
                CommuteToHomeHours = 0.5m,
                LunchHours = 1m,
                HasActivity = true
            },
            new()
            {
                Date = new DateTime(2026, 2, 4),
                WorkHours = 7.5m,
                CommuteToWorkHours = 0.75m,
                CommuteToHomeHours = 0.5m,
                LunchHours = 1m,
                HasActivity = true
            }
        };

        // Act
        var result = _service.GenerateActivityBreakdownChart(breakdown, "Test Week");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 1000);
    }

    [Fact]
    public void GenerateDailyAveragesComparisonChart_WithValidData_ReturnsNonEmptyPngData()
    {
        // Arrange
        var report7Days = new DailyAveragesReport
        {
            AverageWorkHours = 8m,
            AverageCommuteToWorkHours = 0.5m,
            AverageCommuteToHomeHours = 0.5m,
            AverageLunchHours = 1m,
            TotalWorkDays = 5
        };

        var report30Days = new DailyAveragesReport
        {
            AverageWorkHours = 7.8m,
            AverageCommuteToWorkHours = 0.6m,
            AverageCommuteToHomeHours = 0.55m,
            AverageLunchHours = 1m,
            TotalWorkDays = 20
        };

        var report90Days = new DailyAveragesReport
        {
            AverageWorkHours = 7.9m,
            AverageCommuteToWorkHours = 0.55m,
            AverageCommuteToHomeHours = 0.5m,
            AverageLunchHours = 0.9m,
            TotalWorkDays = 60
        };

        // Act
        var result = _service.GenerateDailyAveragesComparisonChart(report7Days, report30Days, report90Days);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 1000);
    }

    [Fact]
    public void GenerateCommutePatternsChart_WithValidData_ReturnsNonEmptyPngData()
    {
        // Arrange
        var toWorkPatterns = new List<CommutePattern>
        {
            new()
            {
                DayOfWeek = DayOfWeek.Monday,
                AverageDurationHours = 0.5m,
                OptimalStartHour = 7,
                ShortestDurationHours = 0.4m,
                SessionCount = 10
            },
            new()
            {
                DayOfWeek = DayOfWeek.Tuesday,
                AverageDurationHours = 0.6m,
                OptimalStartHour = 8,
                ShortestDurationHours = 0.5m,
                SessionCount = 10
            }
        };

        var toHomePatterns = new List<CommutePattern>
        {
            new()
            {
                DayOfWeek = DayOfWeek.Monday,
                AverageDurationHours = 0.55m,
                OptimalStartHour = 17,
                ShortestDurationHours = 0.45m,
                SessionCount = 10
            },
            new()
            {
                DayOfWeek = DayOfWeek.Tuesday,
                AverageDurationHours = 0.65m,
                OptimalStartHour = 18,
                ShortestDurationHours = 0.55m,
                SessionCount = 10
            }
        };

        // Act
        var result = _service.GenerateCommutePatternsChart(toWorkPatterns, toHomePatterns);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 1000);
    }

    [Fact]
    public void GenerateCommutePatternsChart_WithEmptyData_ReturnsEmptyChartPng()
    {
        // Arrange
        var toWorkPatterns = new List<CommutePattern>();
        var toHomePatterns = new List<CommutePattern>();

        // Act
        var result = _service.GenerateCommutePatternsChart(toWorkPatterns, toHomePatterns);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 1000); // Should still generate a chart with "No data" message
    }

    [Fact]
    public void GenerateDailyBreakdownChart_WithOnlyInactiveDays_ReturnsEmptyChartPng()
    {
        // Arrange
        var breakdown = new List<DailyBreakdownRow>
        {
            new() { Date = new DateTime(2026, 2, 3), WorkHours = 0m, HasActivity = false },
            new() { Date = new DateTime(2026, 2, 4), WorkHours = 0m, HasActivity = false }
        };

        // Act
        var result = _service.GenerateDailyBreakdownChart(breakdown, "Inactive Period");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 1000);
    }

    [Fact]
    public void GenerateDailyAveragesComparisonChart_WithZeroData_ReturnsValidPng()
    {
        // Arrange
        var emptyReport = new DailyAveragesReport
        {
            AverageWorkHours = 0m,
            AverageCommuteToWorkHours = 0m,
            AverageCommuteToHomeHours = 0m,
            AverageLunchHours = 0m,
            TotalWorkDays = 0
        };

        // Act
        var result = _service.GenerateDailyAveragesComparisonChart(emptyReport, emptyReport, emptyReport);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length > 1000);
    }
}
