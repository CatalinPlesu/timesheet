using TimeSheet.Core.Application.Models;

namespace TimeSheet.Core.Application.Interfaces.Services;

/// <summary>
/// Service that fetches attendance data from the employer's external API and persists it locally.
/// </summary>
public interface IEmployerImportService
{
    /// <summary>
    /// Imports employer attendance data for the specified user using the provided bearer token.
    /// </summary>
    /// <param name="userId">The ID of the user whose data is being imported.</param>
    /// <param name="bearerToken">The bearer token used to authenticate with the employer's API.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// An <see cref="ImportResult"/> describing how many records were imported, skipped, or whether
    /// the import failed due to an error.
    /// </returns>
    Task<ImportResult> ImportAsync(Guid userId, string bearerToken, CancellationToken ct = default);
}
