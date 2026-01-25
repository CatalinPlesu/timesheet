using TimeSheet.Core.Domain.SharedKernel;

namespace TimeSheet.Core.Application.Queries;

public record GetCurrentStatusQuery
{
  public UserId UserId { get; init; }
  public DateOnly Date { get; init; }

  public GetCurrentStatusQuery(UserId userId, DateOnly date)
  {
    UserId = userId;
    Date = date;
  }
}
