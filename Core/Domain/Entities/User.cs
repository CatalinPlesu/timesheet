using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.SharedKernel;
using TimeSheet.Core.Domain.ValueObjects;

namespace TimeSheet.Core.Domain.Entities;

public class User : Entity<UserId>
{
  private List<ExternalIdentity> identities { get; set; } = [];
  public IReadOnlyCollection<ExternalIdentity> Identities { get => identities.AsReadOnly(); }

  public string FirstName { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
  public UserPreferences Preferences { get; set; } = UserPreferences.CreateDefault();

  public void AddExternalIdentity(IdentityProvider provider, long externalId)
  {
    identities.Add(new ExternalIdentity(provider, externalId));
  }

  public void UpdatePreferences(UserPreferences preferences)
  {
    Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
  }
}

