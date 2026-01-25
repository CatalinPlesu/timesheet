using Microsoft.EntityFrameworkCore;
using TimeSheet.Core.Application.Commands;
using TimeSheet.Core.Application.Queries;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Core.Domain.SharedKernel;
using TimeSheet.Infrastructure.Persistence;
using TimeSheet.Infrastructure.Persistence.Repositories;

// Initialize database
var dbContext = new TimeSheetDbContext(
  new DbContextOptionsBuilder<TimeSheetDbContext>()
    .UseSqlite("Data Source=timesheet.db")
    .Options
);
dbContext.Database.EnsureCreated();

// Initialize repositories and handlers
var userRepo = new UserRepository(dbContext);
var workDayRepo = new WorkDayRepository(dbContext);
var commandHandler = new RecordTransitionCommandHandler(workDayRepo);
var queryHandler = new GetCurrentStatusQueryHandler(workDayRepo);

// Get or create default user
var users = await dbContext.Users.ToListAsync();
var user = users.FirstOrDefault();
if (user == null)
{
  user = User.Create("Default User", 0);
  await userRepo.AddAsync(user);
  await userRepo.SaveChangesAsync();
}

Console.WriteLine("🕐 TimeSheet - Terminal UI");
Console.WriteLine("==========================\n");

// Check for command line arguments
if (args.Length > 0)
{
  await HandleCommand(args[0], user, commandHandler, queryHandler);
  return;
}

// Interactive mode
await RunInteractiveMode(user, commandHandler, queryHandler);

async Task HandleCommand(string command, User user, RecordTransitionCommandHandler handler, GetCurrentStatusQueryHandler queryHandler)
{
  try
  {
    var now = DateTime.UtcNow;
    
    // For context-aware commands, get current state first
    var statusQuery = new GetCurrentStatusQuery(user.Id, DateOnly.FromDateTime(now));
    var currentStatus = await queryHandler.HandleAsync(statusQuery);
    var currentState = currentStatus.CurrentState;
    
    var result = command.ToLower() switch
    {
      "start" or "commute" => await HandleContextualCommute(user, currentState, handler),
      "atwork" => await RecordTransition(user, WorkDayState.AtWork, handler),
      "work" or "working" => await RecordTransition(user, WorkDayState.Working, handler),
      "lunch" => await RecordTransition(user, WorkDayState.OnLunch, handler),
      "home" => await HandleContextualHome(user, currentState, handler),
      "done" or "end" => await RecordTransition(user, WorkDayState.AtHome, handler),
      "emergency" => await RecordTransition(user, WorkDayState.AtHome, handler),
      "sickday" => await RecordTransition(user, WorkDayState.SickDay, handler),
      "vacation" => await RecordTransition(user, WorkDayState.Vacation, handler),
      "status" => await ShowStatus(user, queryHandler),
      "help" => ShowHelp(),
      _ => $"Unknown command: {command}. Use 'help' to see available commands."
    };

    Console.WriteLine(result);
  }
  catch (InvalidOperationException ex)
  {
    Console.WriteLine($"❌ Error: {ex.Message}");
  }
  catch (Exception ex)
  {
    Console.WriteLine($"❌ An error occurred: {ex.Message}");
    if (ex.InnerException != null)
    {
      Console.WriteLine($"   Inner exception: {ex.InnerException.Message}");
    }
  }
}

async Task<string> HandleContextualHome(User user, WorkDayState currentState, RecordTransitionCommandHandler handler)
{
  // Context-aware "home" command:
  // - If commuting home → arrived home (AtHome)
  // - If at work/working/lunch → start commuting home (CommutingHome)
  var targetState = currentState switch
  {
    WorkDayState.CommutingHome => WorkDayState.AtHome,
    _ => WorkDayState.CommutingHome
  };
  
  return await RecordTransition(user, targetState, handler);
}

async Task<string> HandleContextualCommute(User user, WorkDayState currentState, RecordTransitionCommandHandler handler)
{
  // Context-aware "commute" command:
  // - If commuting to work → arrived at work (AtWork)
  // - Otherwise → start commuting to work (CommutingToWork)
  var targetState = currentState switch
  {
    WorkDayState.CommutingToWork => WorkDayState.AtWork,
    _ => WorkDayState.CommutingToWork
  };
  
  return await RecordTransition(user, targetState, handler);
}

async Task RunInteractiveMode(User user, RecordTransitionCommandHandler handler, GetCurrentStatusQueryHandler queryHandler)
{
  Console.WriteLine("Interactive Mode - Type 'help' for commands or 'exit' to quit\n");

  while (true)
  {
    Console.Write("> ");
    var input = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(input))
      continue;

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) || 
        input.Equals("quit", StringComparison.OrdinalIgnoreCase))
    {
      Console.WriteLine("Goodbye! 👋");
      break;
    }

    await HandleCommand(input, user, handler, queryHandler);
    Console.WriteLine();
  }
}

async Task<string> RecordTransition(User user, WorkDayState toState, RecordTransitionCommandHandler handler)
{
  var now = DateTime.UtcNow;
  var command = new RecordTransitionCommand(
    user.Id,
    DateOnly.FromDateTime(now),
    toState,
    TimeOnly.FromDateTime(now)
  );

  await handler.HandleAsync(command);

  var emoji = toState switch
  {
    WorkDayState.CommutingToWork => "🚗",
    WorkDayState.AtWork => "🏢",
    WorkDayState.Working => "💼",
    WorkDayState.OnLunch => "🍽️",
    WorkDayState.CommutingHome => "🚗",
    WorkDayState.AtHome => "🏠",
    WorkDayState.SickDay => "🤒",
    WorkDayState.Vacation => "🏖️",
    _ => "⏱️"
  };

  return $"✅ {emoji} {toState} recorded at {now:HH:mm}";
}

async Task<string> ShowStatus(User user, GetCurrentStatusQueryHandler handler)
{
  var query = new GetCurrentStatusQuery(user.Id, DateOnly.FromDateTime(DateTime.UtcNow));
  var result = await handler.HandleAsync(query);

  var output = $"📊 Current Status: {result.CurrentState}\n";
  output += "━━━━━━━━━━━━━━━━━━━━━━━━\n";

  if (result.Transitions.Any())
  {
    output += "Today's transitions:\n";
    foreach (var transition in result.Transitions)
    {
      var emoji = transition.ToState switch
      {
        WorkDayState.CommutingToWork => "🚗",
        WorkDayState.AtWork => "🏢",
        WorkDayState.Working => "💼",
        WorkDayState.OnLunch => "🍽️",
        WorkDayState.CommutingHome => "🚗",
        WorkDayState.AtHome => "🏠",
        _ => "⏱️"
      };
      output += $"  {emoji} {transition.FromState,-20} → {transition.ToState,-20} at {transition.Timestamp:HH:mm}\n";
    }
  }
  else
  {
    output += "No transitions recorded today.\n";
  }

  return output;
}

string ShowHelp()
{
  return """
  📋 TimeSheet CLI Commands:
  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

  Work Tracking (Context-Aware):
    commute, start    - Start commuting to work / Arrive at work
    work, working     - Start working
    lunch             - Take lunch break
    home              - Start commuting home / Arrive home
    done, end         - Finish work day (force arrive home)
    emergency         - Emergency exit (go home immediately)
    sickday           - Mark as sick day
    vacation          - Mark as vacation

  Information:
    status            - View today's status and transitions
    help              - Show this help message

  Control:
    exit, quit        - Exit the program

  💡 Context-Aware Commands:
    • "commute" twice → first starts commute, second marks arrival
    • "home" twice → first starts commute home, second marks arrival
    
  💡 Quick Start Examples:
    Office work:    commute → commute → work → lunch → home → home
    Compact flow:   commute → work → lunch → home (auto-fills states!)
    Remote work:    work → lunch → done
    
  ℹ️  Note: Commands adapt to your current state. The system
      automatically fills in implied transitions as needed.
  """;
}
