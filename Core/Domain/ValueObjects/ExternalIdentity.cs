using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Core.Domain.ValueObjects;

public record ExternalIdentity(IdentityProvider IdentityProvider, long Id);
