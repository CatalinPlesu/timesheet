using Microsoft.EntityFrameworkCore;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Core.Domain.SharedKernel;

namespace TimeSheet.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
  private readonly TimeSheetDbContext _context;

  public UserRepository(TimeSheetDbContext context)
  {
    _context = context ?? throw new ArgumentNullException(nameof(context));
  }

  public async Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default)
  {
    return await _context.Users
      .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
  }

  public async Task<User?> GetByExternalIdAsync(long externalId, CancellationToken cancellationToken = default)
  {
    return await _context.Users
      .Where(u => u.Identities.Any(i => i.Id == externalId))
      .FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
  {
    await _context.Users.AddAsync(user, cancellationToken);
    return user;
  }

  public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    await _context.SaveChangesAsync(cancellationToken);
  }
}
