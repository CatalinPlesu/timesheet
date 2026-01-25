using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.SharedKernel;

namespace TimeSheet.Infrastructure.Persistence.Configurations;

public class WorkDayConfiguration : IEntityTypeConfiguration<WorkDay>
{
  public void Configure(EntityTypeBuilder<WorkDay> entity)
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
  }
}
