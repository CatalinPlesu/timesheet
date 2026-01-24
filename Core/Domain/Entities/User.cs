using TimeSheet.Core.Domain.SharedKernel;
using TimeSheet.Core.Domain.ValueObjects;

namespace TimeSheet.Core.Domain.Entities;

public class User : Entity<UserId>
{
  private List<ExternalIdentity> identityes { get; set; } = [];
  public IReadOnlyCollection<ExternalIdentity> Identities { get => identityes.AsReadOnly(); }

  public string FirstName { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
}

