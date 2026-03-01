using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
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
/// </summary>
public class EmployerImportService : IEmployerImportService
{
    private readonly EmployerApiOptions _options;
    private readonly IEmployerAttendanceRepository _repo;
    private readonly HttpClient _httpClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmployerImportService> _logger;

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
    /// <param name="logger">Logger for debug diagnostics.</param>
    public EmployerImportService(
        IOptions<EmployerApiOptions> options,
        IEmployerAttendanceRepository repo,
        HttpClient httpClient,
        IUnitOfWork unitOfWork,
        ILogger<EmployerImportService> logger)
    {
        _options = options.Value;
        _repo = repo;
        _httpClient = httpClient;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Normalises a <see cref="DateTime"/> value received from the Timily API to UTC.
    /// <para>
    /// Timily sends clock timestamps as naive ISO-8601 strings without a timezone suffix
    /// (e.g. <c>"2026-02-13T07:00:21"</c>).  <c>System.Text.Json</c> deserialises these
    /// as <see cref="DateTimeKind.Unspecified"/>.  The EF Core write-converter in
    /// <c>AppDbContext</c> then calls <c>ToUniversalTime()</c> on any non-UTC value,
    /// which subtracts the server's local UTC offset a second time — producing timestamps
    /// that are off by the server's UTC offset (e.g. 2 h in EET).
    /// </para>
    /// <para>
    /// The fix: treat every incoming Timily timestamp as UTC regardless of the <c>Kind</c>
    /// flag, by calling <see cref="DateTime.SpecifyKind"/> before handing it to the domain
    /// model.  When the value already carries <see cref="DateTimeKind.Utc"/> (e.g. if
    /// Timily ever adds a <c>Z</c> suffix) or <see cref="DateTimeKind.Local"/> (offset
    /// present in JSON), we first normalise to UTC via <c>ToUniversalTime()</c> so the
    /// wall-clock value is preserved, then re-tag as <c>Utc</c>.
    /// </para>
    /// </summary>
    private static DateTime? ToUtc(DateTime? dt)
    {
        if (dt is null) return null;

        var utc = dt.Value.Kind switch
        {
            DateTimeKind.Utc => dt.Value,
            DateTimeKind.Local => dt.Value.ToUniversalTime(),
            // Unspecified: Timily sends UTC without Z — treat as-is, just re-tag.
            _ => DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc)
        };

        return utc;
    }

    /// <inheritdoc/>
    public async Task<ImportResult> ImportAsync(
        Guid userId,
        string bearerToken,
        CancellationToken ct = default)
    {
        // 1. Check configuration
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            return new ImportResult(0, 0, 0,
                Error: "Employer API not configured. Set EmployerApi:BaseUrl in appsettings.");

        // 2. Call the employer API
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
        string rawJson = string.Empty;
        try
        {
            rawJson = await response.Content.ReadAsStringAsync(ct);
            _logger.LogDebug("Timily raw JSON response: {Json}", rawJson);
            data = JsonSerializer.Deserialize<TimilyAttendanceResponse>(rawJson, JsonOptions);
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

                var clockInUtc  = ToUtc(clockInOut?.StartDate);
                var clockOutUtc = ToUtc(clockInOut?.EndDate);

                if (clockInOut is not null)
                {
                    _logger.LogDebug(
                        "Day {Date}: raw StartDate={RawStart} (Kind={RawKind}) → stored UTC={UtcStart}; " +
                        "raw EndDate={RawEnd} (Kind={RawEndKind}) → stored UTC={UtcEnd}",
                        date,
                        clockInOut.StartDate, clockInOut.StartDate?.Kind, clockInUtc,
                        clockInOut.EndDate,   clockInOut.EndDate?.Kind,   clockOutUtc);
                }

                var record = EmployerAttendanceRecord.Create(
                    userId,
                    date,
                    clockIn: clockInUtc,
                    clockOut: clockOutUtc,
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
