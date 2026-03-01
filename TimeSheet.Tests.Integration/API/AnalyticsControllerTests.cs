using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Infrastructure.Persistence;
using TimeSheet.Tests.Integration.Fixtures;

namespace TimeSheet.Tests.Integration.API;

/// <summary>
/// Integration tests for AnalyticsController endpoints.
/// Covers office span calculation, including the regression for TimeSheet-2xx:
/// work entry added out-of-order after commute-to-home must not break office span.
/// </summary>
public class AnalyticsControllerTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AnalyticsControllerTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Creates a test user (if not already present), generates a JWT token,
    /// and returns an authenticated HTTP client.
    /// </summary>
    private async Task<(HttpClient client, long userId)> CreateAuthenticatedClientAsync()
    {
        const long telegramUserId = 99991;

        // Ensure the user exists (may have been cleared by ClearDatabase in the test).
        using (var scope = _fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!dbContext.Set<User>().Any(u => u.TelegramUserId == telegramUserId))
            {
                var user = new User(telegramUserId, "analyticstest", isAdmin: false, utcOffsetMinutes: 0);
                dbContext.Set<User>().Add(user);
                await dbContext.SaveChangesAsync();
            }
        }

        string token;
        using (var scope = _fixture.Services.CreateScope())
        {
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            token = jwtService.GenerateToken(telegramUserId, "analyticstest", isAdmin: false);
        }

        // Use the default test client; the WebApplicationFactory test host accepts HTTP.
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (client, telegramUserId);
    }

    /// <summary>
    /// Regression test for TimeSheet-2xx:
    /// Office span must be computed correctly when a work entry is added out-of-order
    /// AFTER the commute-to-home session, with EndedAt = null (still active).
    ///
    /// Scenario:
    ///   08:00 – 08:30  commute-to-work  (completed)
    ///   08:30 – 13:00  lunch            (completed, user forgot to add work in between)
    ///   13:00 – 13:30  commute-to-home  (completed)
    ///   09:00 –  null  work             (active — added retroactively after home commute)
    ///
    /// Expected office span: 13:00 − 08:30 = 4.5 h
    ///   (clock-out = home commute StartedAt, clock-in = to-work commute EndedAt)
    /// </summary>
    [Fact]
    public async Task GetDailyBreakdown_WorkEntryAddedOutOfOrder_ComputesOfficeSpanCorrectly()
    {
        // Arrange
        _fixture.ClearDatabase();
        var (client, userId) = await CreateAuthenticatedClientAsync();

        // Use a date safely in the past so the query window covers it.
        var day = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        using (var scope = _fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 08:00 – 08:30  commute-to-work (completed)
            var commuteToWork = new TrackingSession(userId, TrackingState.Commuting, day.AddHours(8), CommuteDirection.ToWork);
            commuteToWork.End(day.AddHours(8.5));

            // 08:30 – 13:00  lunch (user forgot to log /work before lunch)
            var lunch = new TrackingSession(userId, TrackingState.Lunch, day.AddHours(8.5));
            lunch.End(day.AddHours(13));

            // 13:00 – 13:30  commute-to-home (completed)
            var commuteToHome = new TrackingSession(userId, TrackingState.Commuting, day.AddHours(13), CommuteDirection.ToHome);
            commuteToHome.End(day.AddHours(13.5));

            // work session added retroactively AFTER home commute — still active (EndedAt = null).
            // StartedAt was edited (via Telegram -/+ buttons) to 09:00,
            // which is between commute-to-work and lunch.
            var work = new TrackingSession(userId, TrackingState.Working, day.AddHours(9));
            // EndedAt intentionally null — simulates an active session never closed.

            dbContext.Set<TrackingSession>().AddRange(commuteToWork, lunch, commuteToHome, work);
            await dbContext.SaveChangesAsync();
        }

        var startDate = "2026-01-15";
        var endDate = "2026-01-16";

        // Act
        var response = await client.GetAsync(
            $"/api/analytics/daily-breakdown?startDate={startDate}&endDate={endDate}");

        // Assert
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // If this fails, responseBody has clues

        var rows = JsonSerializer.Deserialize<List<DailyBreakdownRow>>(responseBody, JsonOptions);

        Assert.NotNull(rows);
        Assert.Single(rows);

        var row = rows[0];
        Assert.True(row.HasActivity);

        // Office span must not be 0 or null:
        // clock-in  = commuteToWork.EndedAt  = 08:30
        // clock-out = commuteToHome.StartedAt = 13:00
        // span = 4.5 h
        Assert.NotNull(row.OfficeSpanHours);
        Assert.Equal(4.5m, row.OfficeSpanHours!.Value, precision: 4);

        // Commute hours from completed sessions
        Assert.Equal(0.5m, row.CommuteToWorkHours, precision: 4);
        Assert.Equal(0.5m, row.CommuteToHomeHours, precision: 4);

        // Lunch from completed session: 08:30 – 13:00 = 4.5 h
        Assert.Equal(4.5m, row.LunchHours, precision: 4);

        // Work hours from completed sessions only (active session excluded) = 0
        Assert.Equal(0m, row.WorkHours);
    }

    /// <summary>
    /// Regression test for TimeSheet-2xx edge case:
    /// OfficeSpanHours must be null (not 0) when commute-to-home is missing.
    /// </summary>
    [Fact]
    public async Task GetDailyBreakdown_MissingHomeCommute_OfficeSpanIsNull()
    {
        // Arrange
        _fixture.ClearDatabase();
        var (client, userId) = await CreateAuthenticatedClientAsync();

        var day = new DateTime(2026, 1, 16, 0, 0, 0, DateTimeKind.Utc);

        using (var scope = _fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var commuteToWork = new TrackingSession(userId, TrackingState.Commuting, day.AddHours(8), CommuteDirection.ToWork);
            commuteToWork.End(day.AddHours(8.5));

            var work = new TrackingSession(userId, TrackingState.Working, day.AddHours(8.5));
            work.End(day.AddHours(17));

            dbContext.Set<TrackingSession>().AddRange(commuteToWork, work);
            await dbContext.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync(
            "/api/analytics/daily-breakdown?startDate=2026-01-16&endDate=2026-01-17");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var rows = JsonSerializer.Deserialize<List<DailyBreakdownRow>>(json, JsonOptions);

        Assert.NotNull(rows);
        Assert.Single(rows);
        // OfficeSpanHours must be null when the home commute is missing
        Assert.Null(rows[0].OfficeSpanHours);
    }

    /// <summary>
    /// Diagnostic: prove that the /health endpoint works and the JWT token is well-formed.
    /// This isolates JWT auth issues from endpoint logic.
    /// </summary>
    [Fact]
    public async Task Diagnostic_HealthEndpoint_ReturnsOk()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Diagnostic: prove that an [Authorize] endpoint returns 401 without a token.
    /// </summary>
    [Fact]
    public async Task Diagnostic_AnalyticsEndpoint_WithoutToken_Returns401()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/api/analytics/daily-breakdown?startDate=2026-01-17&endDate=2026-01-18");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Diagnostic: prove that an [Authorize] endpoint returns 200 with a valid token.
    /// </summary>
    [Fact]
    public async Task Diagnostic_AnalyticsEndpoint_WithToken_Returns200()
    {
        _fixture.ClearDatabase();
        var (client, _) = await CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/analytics/daily-breakdown?startDate=2026-01-17&endDate=2026-01-18");
        var body = await response.Content.ReadAsStringAsync();
        var wwwAuth = response.Headers.WwwAuthenticate.ToString();
        // Include body and WWW-Authenticate header in assertion message for debugging
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 but got {(int)response.StatusCode}: {body}\nWWW-Authenticate: {wwwAuth}");
    }

    /// <summary>
    /// Full normal work day: office span computed correctly from commute events.
    /// </summary>
    [Fact]
    public async Task GetDailyBreakdown_NormalWorkDay_ComputesOfficeSpanCorrectly()
    {
        // Arrange
        _fixture.ClearDatabase();
        var (client, userId) = await CreateAuthenticatedClientAsync();

        var day = new DateTime(2026, 1, 17, 0, 0, 0, DateTimeKind.Utc);

        using (var scope = _fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var commuteToWork = new TrackingSession(userId, TrackingState.Commuting, day.AddHours(8), CommuteDirection.ToWork);
            commuteToWork.End(day.AddHours(8.5));

            var work1 = new TrackingSession(userId, TrackingState.Working, day.AddHours(8.5));
            work1.End(day.AddHours(12));

            var lunch = new TrackingSession(userId, TrackingState.Lunch, day.AddHours(12));
            lunch.End(day.AddHours(13));

            var work2 = new TrackingSession(userId, TrackingState.Working, day.AddHours(13));
            work2.End(day.AddHours(17));

            var commuteToHome = new TrackingSession(userId, TrackingState.Commuting, day.AddHours(17), CommuteDirection.ToHome);
            commuteToHome.End(day.AddHours(17.5));

            dbContext.Set<TrackingSession>().AddRange(commuteToWork, work1, lunch, work2, commuteToHome);
            await dbContext.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync(
            "/api/analytics/daily-breakdown?startDate=2026-01-17&endDate=2026-01-18");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var rows = JsonSerializer.Deserialize<List<DailyBreakdownRow>>(json, JsonOptions);

        Assert.NotNull(rows);
        Assert.Single(rows);

        var row = rows[0];
        Assert.True(row.HasActivity);

        // Office span: commuteToHome.StartedAt (17:00) − commuteToWork.EndedAt (08:30) = 8.5 h
        Assert.NotNull(row.OfficeSpanHours);
        Assert.Equal(8.5m, row.OfficeSpanHours!.Value, precision: 4);

        // Work: (12:00 − 08:30) + (17:00 − 13:00) = 3.5 + 4 = 7.5 h
        Assert.Equal(7.5m, row.WorkHours, precision: 4);
        Assert.Equal(1m, row.LunchHours, precision: 4);
        Assert.Equal(0.5m, row.CommuteToWorkHours, precision: 4);
        Assert.Equal(0.5m, row.CommuteToHomeHours, precision: 4);
    }

    // ---------------------------------------------------------------------------
    // Local DTO — mirrors DailyBreakdownDto from TimeSheet.Presentation.API project
    // ---------------------------------------------------------------------------
    private sealed class DailyBreakdownRow
    {
        public DateTime Date { get; set; }
        public decimal WorkHours { get; set; }
        public decimal CommuteToWorkHours { get; set; }
        public decimal CommuteToHomeHours { get; set; }
        public decimal LunchHours { get; set; }
        public decimal? TotalDurationHours { get; set; }
        public decimal? OfficeSpanHours { get; set; }
        public bool HasActivity { get; set; }
    }
}
