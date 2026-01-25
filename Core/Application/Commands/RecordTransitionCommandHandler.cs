using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Repositories;

namespace TimeSheet.Core.Application.Commands;

public class RecordTransitionCommandHandler
{
  private readonly IWorkDayRepository _workDayRepository;

  public RecordTransitionCommandHandler(IWorkDayRepository workDayRepository)
  {
    _workDayRepository = workDayRepository ?? throw new ArgumentNullException(nameof(workDayRepository));
  }

  public async Task HandleAsync(RecordTransitionCommand command, CancellationToken cancellationToken = default)
  {
    // Find or create WorkDay
    var workDay = await _workDayRepository.GetByUserAndDateAsync(command.UserId, command.Date, cancellationToken);

    if (workDay == null)
    {
      workDay = WorkDay.Create(command.UserId, command.Date);
      await _workDayRepository.AddAsync(workDay, cancellationToken);
    }

    // Record the transition
    workDay.RecordTransition(command.ToState, command.Timestamp);

    // Save changes
    await _workDayRepository.SaveChangesAsync(cancellationToken);
  }
}
