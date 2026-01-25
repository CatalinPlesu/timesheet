using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.Repositories;

namespace TimeSheet.Core.Application.Queries;

public class GetCurrentStatusQueryHandler
{
  private readonly IWorkDayRepository _workDayRepository;

  public GetCurrentStatusQueryHandler(IWorkDayRepository workDayRepository)
  {
    _workDayRepository = workDayRepository ?? throw new ArgumentNullException(nameof(workDayRepository));
  }

  public async Task<CurrentStatusResult> HandleAsync(GetCurrentStatusQuery query, CancellationToken cancellationToken = default)
  {
    var workDay = await _workDayRepository.GetByUserAndDateAsync(query.UserId, query.Date, cancellationToken);

    if (workDay == null)
    {
      return new CurrentStatusResult(WorkDayState.NotStarted, Array.Empty<Domain.Entities.StateTransition>());
    }

    return new CurrentStatusResult(workDay.CurrentState, workDay.Transitions.ToList());
  }
}
