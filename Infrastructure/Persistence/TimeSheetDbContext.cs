using Microsoft.EntityFrameworkCore;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.SharedKernel;

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

    // User configuration
    modelBuilder.Entity<User>(entity =>
    {
      entity.HasKey(u => u.Id);
      entity.Property(u => u.Id)
        .HasConversion(
          id => id.Value,
          value => UserId.From(value));

      entity.Property(u => u.Name).IsRequired().HasMaxLength(200);
      entity.Property(u => u.UtcOffsetHours).IsRequired();

      // Configure external identities as owned entities
      entity.OwnsMany(u => u.Identities, identityBuilder =>
      {
        identityBuilder.WithOwner();
        identityBuilder.Property<int>("InternalId");
        identityBuilder.HasKey("InternalId");
        identityBuilder.Property(i => i.IdentityProvider).IsRequired();
        identityBuilder.Property(i => i.Id).IsRequired();
        identityBuilder.ToTable("UserIdentities");
      });
      
      // Configure to use the private backing field for Identities
      entity.Navigation(u => u.Identities).UsePropertyAccessMode(PropertyAccessMode.Field);
    });

    // WorkDay configuration
    modelBuilder.Entity<WorkDay>(entity =>
    {
      entity.HasKey(w => w.Id);
      entity.Property(w => w.Id)
        .HasConversion(
          id => id.Value,
          value => WorkDayId.From(value));

      entity.Property(w => w.UserId)
        .HasConversion(
          id => id.Value,
          value => UserId.From(value))
        .IsRequired();

      entity.Property(w => w.Date).IsRequired();

      // Unique constraint on UserId + Date
      entity.HasIndex(w => new { w.UserId, w.Date }).IsUnique();

      // Ignore the public Transitions property and configure the _transitions field
      entity.Ignore(w => w.Transitions);
      
      // Configure StateTransitions as owned entities using the _transitions field
      entity.OwnsMany<StateTransition>("_transitions", transitionBuilder =>
      {
        transitionBuilder.WithOwner();
        transitionBuilder.Property<int>("InternalId");
        transitionBuilder.HasKey("InternalId");
        transitionBuilder.Property(t => t.FromState).IsRequired();
        transitionBuilder.Property(t => t.ToState).IsRequired();
        transitionBuilder.Property(t => t.Timestamp).IsRequired();
        transitionBuilder.ToTable("StateTransitions");
      });
    });
  }
}
