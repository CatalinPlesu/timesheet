using Microsoft.EntityFrameworkCore;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Core.Domain.SharedKernel;

namespace TimeSheet.Infrastructure.Persistence.Repositories;

public class WorkDayRepository : IWorkDayRepository
{
  private readonly TimeSheetDbContext _context;

  public WorkDayRepository(TimeSheetDbContext context)
  {
    _context = context ?? throw new ArgumentNullException(nameof(context));
  }

  public async Task<WorkDay?> GetByUserAndDateAsync(UserId userId, DateOnly date, CancellationToken cancellationToken = default)
  {
    // Owned entities (Transitions) are automatically loaded by EF Core
    return await _context.WorkDays
      .FirstOrDefaultAsync(w => w.UserId == userId && w.Date == date, cancellationToken);
  }

  public async Task<WorkDay> AddAsync(WorkDay workDay, CancellationToken cancellationToken = default)
  {
    await _context.WorkDays.AddAsync(workDay, cancellationToken);
    return workDay;
  }

  public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    await _context.SaveChangesAsync(cancellationToken);
  }
}
