using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.SharedKernel;

namespace TimeSheet.Core.Domain.Repositories;

public interface IUserRepository
{
  Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default);
  Task<User?> GetByExternalIdAsync(long externalId, CancellationToken cancellationToken = default);
  Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
  Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
