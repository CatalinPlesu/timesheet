using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TimeSheet.Core.Application.ExternalApi.Timily;
using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Application.Models;
using TimeSheet.Core.Application.Options;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Repositories;

namespace TimeSheet.Core.Application.Services;

/// <summary>
/// Fetches attendance data from the employer's external API (Timily) and persists it idempotently.
/// Enforces a 7-day rate limit between imports per user.
/// </summary>
public class EmployerImportService : IEmployerImportService
{
    private readonly EmployerApiOptions _options;
    private readonly IEmployerAttendanceRepository _repo;
    private readonly HttpClient _httpClient;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of <see cref="EmployerImportService"/>.
    /// </summary>
    /// <param name="options">Employer API configuration options.</param>
    /// <param name="repo">Repository for attendance records and import logs.</param>
    /// <param name="httpClient">HTTP client used to call the employer API.</param>
    /// <param name="unitOfWork">Unit of work for committing changes.</param>
    public EmployerImportService(
        IOptions<EmployerApiOptions> options,
        IEmployerAttendanceRepository repo,
        HttpClient httpClient,
        IUnitOfWork unitOfWork)
    {
        _options = options.Value;
        _repo = repo;
        _httpClient = httpClient;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<ImportResult> ImportAsync(
        Guid userId,
        string bearerToken,
        bool force = false,
        CancellationToken ct = default)
    {
        // 1. Check configuration
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            return new ImportResult(0, 0, 0,
                Error: "Employer API not configured. Set EmployerApi:BaseUrl in appsettings.");

        // 2. Rate limit check (unless force=true)
        if (!force)
        {
            var lastImport = await _repo.GetLastImportAsync(userId, ct);
            if (lastImport != null)
            {
                var daysSince = (DateTimeOffset.UtcNow - lastImport.CreatedAt).TotalDays;
                if (daysSince < 7)
                {
                    var daysRemaining = (int)Math.Ceiling(7 - daysSince);
                    return new ImportResult(0, 0, 0,
                        RateLimited: true,
                        RateLimitDaysRemaining: daysRemaining);
                }
            }
        }

        // 3. Call the employer API
        var url = $"{_options.BaseUrl.TrimEnd('/')}{_options.AttendanceEndpoint}?dateOnly={DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            return new ImportResult(0, 0, 0, Error: $"API call failed: {ex.Message}");
        }

        if (!response.IsSuccessStatusCode)
            return new ImportResult(0, 0, 0, Error: $"API returned {(int)response.StatusCode}");

        // 4. Deserialize the response
        TimilyAttendanceResponse? data;
        try
        {
            var json = await response.Content.ReadAsStringAsync(ct);
            data = JsonSerializer.Deserialize<TimilyAttendanceResponse>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            return new ImportResult(0, 0, 0, Error: $"Parse failed: {ex.Message}");
        }

        if (data == null)
            return new ImportResult(0, 0, 0, Error: "Empty response");

        // 5. Process days and upsert records
        var importBatchId = Guid.NewGuid().ToString();
        int imported = 0, skipped = 0, total = 0;

        foreach (var month in data.Months)
        {
            foreach (var day in month.AttendanceDays)
            {
                total++;

                var date = DateOnly.FromDateTime(day.DateOnly);
                var clockInOut = day.Events.FirstOrDefault(e => e.EventType == "ClockInOutDoor");
                var eventTypes = string.Join(",", day.Events.Select(e => e.EventType).Distinct());

                // Skip days with no meaningful data (weekends, not-employed, etc.)
                bool hasWork = day.Events.Any(e =>
                    e.EventType == "ClockInOutDoor" || e.EventType == "Absence");

                if (!hasWork)
                {
                    skipped++;
                    continue;
                }

                var record = EmployerAttendanceRecord.Create(
                    userId,
                    date,
                    clockIn: clockInOut?.StartDate,
                    clockOut: clockInOut?.EndDate,
                    workingHours: clockInOut?.WorkingHours,
                    hasConflict: day.ConflictType != "None",
                    conflictType: day.ConflictType == "None" ? null : day.ConflictType,
                    eventTypes: eventTypes,
                    importBatchId: importBatchId);

                await _repo.UpsertAsync(record, ct);
                imported++;
            }
        }

        // 6. Log the import operation
        var log = EmployerImportLog.Create(userId, imported, skipped, total);
        await _repo.AddImportLogAsync(log, ct);
        await _unitOfWork.CompleteAsync(ct);

        return new ImportResult(imported, skipped, total);
    }
}
