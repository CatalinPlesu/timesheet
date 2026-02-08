using TimeSheet.Core.Application.Options;

namespace TimeSheet.Presentation.Telegram.Options;

public class WorkerOptions : IOptionsWithSectionName
{
    public static string SectionName => "Workers";

    public TimeSpan AutoShutdownCheckInterval { get; set; } = TimeSpan.FromMinutes(3);
    public TimeSpan ForgotShutdownCheckInterval { get; set; } = TimeSpan.FromMinutes(3);
    public TimeSpan LunchReminderCheckInterval { get; set; } = TimeSpan.FromMinutes(3);
    public TimeSpan WorkHoursAlertCheckInterval { get; set; } = TimeSpan.FromMinutes(3);
}
