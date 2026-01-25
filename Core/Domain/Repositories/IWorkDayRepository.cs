using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.SharedKernel;

namespace TimeSheet.Core.Domain.Repositories;

public interface IWorkDayRepository
{
  Task<WorkDay?> GetByUserAndDateAsync(UserId userId, DateOnly date, CancellationToken cancellationToken = default);
  Task<WorkDay> AddAsync(WorkDay workDay, CancellationToken cancellationToken = default);
  Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
