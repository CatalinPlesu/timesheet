using Microsoft.EntityFrameworkCore;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Infrastructure.Persistence.Configurations;

namespace TimeSheet.Infrastructure.Persistence;

public class TimeSheetDbContext : DbContext
{
  public DbSet<User> Users { get; set; } = null!;
  public DbSet<WorkDay> WorkDays { get; set; } = null!;

  public TimeSheetDbContext(DbContextOptions<TimeSheetDbContext> options) : base(options)
  {
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.ApplyConfiguration(new UserConfiguration());
    modelBuilder.ApplyConfiguration(new WorkDayConfiguration());
  }
}
