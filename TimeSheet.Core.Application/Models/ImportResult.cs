namespace TimeSheet.Core.Application.Models;

/// <summary>
/// The result of an employer attendance import operation.
/// </summary>
/// <param name="Imported">Number of days that were successfully upserted.</param>
/// <param name="Skipped">Number of days that were skipped (no meaningful data).</param>
/// <param name="TotalDaysProcessed">Total number of days seen in the API response.</param>
/// <param name="Error">Error message if the import failed; null on success.</param>
/// <param name="RateLimited">True when the import was blocked by the 7-day rate limit.</param>
/// <param name="RateLimitDaysRemaining">Days until the next import is allowed (set when <paramref name="RateLimited"/> is true).</param>
public record ImportResult(
    int Imported,
    int Skipped,
    int TotalDaysProcessed,
    string? Error = null,
    bool RateLimited = false,
    int? RateLimitDaysRemaining = null
);
