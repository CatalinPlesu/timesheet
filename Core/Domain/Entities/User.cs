using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.SharedKernel;
using TimeSheet.Core.Domain.ValueObjects;

namespace TimeSheet.Core.Domain.Entities;

public class User : Entity<UserId>
{
  private List<ExternalIdentity> identities { get; set; } = [];
  public IReadOnlyCollection<ExternalIdentity> Identities { get => identities.AsReadOnly(); }

  public string Name { get; private set; } = string.Empty;
  public int UtcOffsetHours { get; private set; }

  // Private constructor for EF Core
  private User() { }

  public static User Create(string name, int utcOffsetHours)
  {
    if (string.IsNullOrWhiteSpace(name))
    {
      throw new ArgumentException("Name cannot be empty", nameof(name));
    }

    if (utcOffsetHours < -12 || utcOffsetHours > 14)
    {
      throw new ArgumentException("UTC offset must be between -12 and +14", nameof(utcOffsetHours));
    }

    return new User
    {
      Name = name,
      UtcOffsetHours = utcOffsetHours
    };
  }

  public void AddExternalIdentity(IdentityProvider provider, long externalId)
  {
    identities.Add(new ExternalIdentity(provider, externalId));
  }
}

