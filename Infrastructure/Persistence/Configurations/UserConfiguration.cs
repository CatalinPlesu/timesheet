using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.SharedKernel;

namespace TimeSheet.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
  public void Configure(EntityTypeBuilder<User> entity)
  {
    entity.HasKey(u => u.Id);
    entity.Property(u => u.Id)
      .HasConversion(
        id => id.Value,
        value => UserId.From(value));

    entity.Property(u => u.Name).IsRequired().HasMaxLength(200);
    entity.Property(u => u.UtcOffsetHours).IsRequired();

    // Configure notification preferences as owned entity
    entity.OwnsOne(u => u.NotificationPreferences, prefs =>
    {
      prefs.Property(p => p.LunchReminderEnabled).IsRequired();
      prefs.Property(p => p.LunchReminderTime).IsRequired();
      prefs.Property(p => p.EndOfDayReminderEnabled).IsRequired();
      prefs.Property(p => p.EndOfDayReminderTime).IsRequired();
      prefs.Property(p => p.ClockOutReminderEnabled).IsRequired();
      prefs.Property(p => p.GoalAchievedNotificationEnabled).IsRequired();
    });

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
  }
}
