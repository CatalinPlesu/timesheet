using TimeSheet.Core.Application.Models;

namespace TimeSheet.Core.Application.Interfaces.Services;

/// <summary>
/// Service that fetches attendance data from the employer's external API and persists it locally.
/// Enforces a 7-day rate limit between imports to avoid hammering the employer's API.
/// </summary>
public interface IEmployerImportService
{
    /// <summary>
    /// Imports employer attendance data for the specified user using the provided bearer token.
    /// </summary>
    /// <param name="userId">The ID of the user whose data is being imported.</param>
    /// <param name="bearerToken">The bearer token used to authenticate with the employer's API.</param>
    /// <param name="force">
    /// When <c>true</c>, bypasses the 7-day rate limit and imports regardless of when the last import occurred.
    /// </param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// An <see cref="ImportResult"/> describing how many records were imported, skipped, or whether
    /// the import was blocked by the rate limit or failed due to an error.
    /// </returns>
    Task<ImportResult> ImportAsync(Guid userId, string bearerToken, bool force = false, CancellationToken ct = default);
}
