using TimeSheet.Core.Domain.SharedKernel;

namespace TimeSheet.Core.Domain.Entities;

/// <summary>
/// Records a summary of an employer attendance data import operation.
/// Each import run creates one log entry per user processed.
/// </summary>
public sealed class EmployerImportLog : CreatedEntity
{
    /// <summary>
    /// Gets the ID of the user whose attendance data was imported.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the number of attendance records successfully imported (upserted) in this run.
    /// </summary>
    public int RecordsImported { get; private set; }

    /// <summary>
    /// Gets the number of attendance records skipped during this run (e.g., duplicates, invalid data).
    /// </summary>
    public int RecordsSkipped { get; private set; }

    /// <summary>
    /// Gets the total number of days processed from the employer API response in this run.
    /// </summary>
    public int TotalDaysProcessed { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployerImportLog"/> class.
    /// Used by EF Core for entity materialization.
    /// </summary>
    private EmployerImportLog() { }

    /// <summary>
    /// Creates a new employer import log entry.
    /// </summary>
    /// <param name="userId">The ID of the user whose data was imported.</param>
    /// <param name="imported">The number of records successfully imported.</param>
    /// <param name="skipped">The number of records skipped.</param>
    /// <param name="total">The total number of days processed.</param>
    /// <returns>A new <see cref="EmployerImportLog"/> instance.</returns>
    public static EmployerImportLog Create(Guid userId, int imported, int skipped, int total)
    {
        return new EmployerImportLog
        {
            UserId = userId,
            RecordsImported = imported,
            RecordsSkipped = skipped,
            TotalDaysProcessed = total
        };
    }
}
