using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TimeSheet.Core.Application.Commands;
using TimeSheet.Core.Application.Queries;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Core.Domain.SharedKernel;
using TimeSheet.Infrastructure.Persistence;
using TimeSheet.Infrastructure.Persistence.Repositories;
using DomainUser = TimeSheet.Core.Domain.Entities.User;

var builder = Host.CreateApplicationBuilder(args);

// Get bot token from environment variable
var botToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
if (string.IsNullOrEmpty(botToken))
{
  Console.WriteLine("Error: TELEGRAM_BOT_TOKEN environment variable is not set.");
  Console.WriteLine("Please set it before running the bot:");
  Console.WriteLine("  export TELEGRAM_BOT_TOKEN=your_bot_token_here");
  return;
}

// Register services
builder.Services.AddDbContext<TimeSheetDbContext>(options =>
  options.UseSqlite("Data Source=timesheet.db"));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWorkDayRepository, WorkDayRepository>();
builder.Services.AddScoped<RecordTransitionCommandHandler>();
builder.Services.AddScoped<GetCurrentStatusQueryHandler>();

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.AddHostedService<BotService>();

var host = builder.Build();

// Ensure database is created
using (var scope = host.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<TimeSheetDbContext>();
  db.Database.EnsureCreated();
}

await host.RunAsync();

// Bot Service
class BotService : BackgroundService
{
  private readonly ITelegramBotClient _bot;
  private readonly IServiceProvider _services;

  public BotService(ITelegramBotClient bot, IServiceProvider services)
  {
    _bot = bot;
    _services = services;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var me = await _bot.GetMe(stoppingToken);
    Console.WriteLine($"Bot started: @{me.Username}");

    var receiverOptions = new ReceiverOptions
    {
      AllowedUpdates = [UpdateType.Message]
    };

    _bot.StartReceiving(
      HandleUpdateAsync,
      HandleErrorAsync,
      receiverOptions,
      stoppingToken
    );

    await Task.Delay(Timeout.Infinite, stoppingToken);
  }

  private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
  {
    if (update.Message is not { } message || message.Text is not { } text)
      return;

    var chatId = message.Chat.Id;
    var userId = message.From!.Id;

    using var scope = _services.CreateScope();
    var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var commandHandler = scope.ServiceProvider.GetRequiredService<RecordTransitionCommandHandler>();
    var queryHandler = scope.ServiceProvider.GetRequiredService<GetCurrentStatusQueryHandler>();

    // Ensure user exists
    var user = await userRepo.GetByExternalIdAsync(userId, cancellationToken);
    if (user == null && !text.StartsWith("/start"))
    {
      await bot.SendMessage(chatId, "Please start with /start command first.", cancellationToken: cancellationToken);
      return;
    }

    try
    {
      var response = text.Split(' ')[0] switch
      {
        "/start" => await HandleStart(userId, message.From.FirstName, userRepo, cancellationToken),
        "/commute" => await HandleTransition(user!, WorkDayState.CommutingToWork, commandHandler, cancellationToken),
        "/atwork" => await HandleTransition(user!, WorkDayState.AtWork, commandHandler, cancellationToken),
        "/work" => await HandleTransition(user!, WorkDayState.Working, commandHandler, cancellationToken),
        "/lunch" => await HandleTransition(user!, WorkDayState.OnLunch, commandHandler, cancellationToken),
        "/home" => await HandleTransition(user!, WorkDayState.CommutingHome, commandHandler, cancellationToken),
        "/done" => await HandleTransition(user!, WorkDayState.AtHome, commandHandler, cancellationToken),
        "/emergency" => await HandleTransition(user!, WorkDayState.AtHome, commandHandler, cancellationToken),
        "/sickday" => await HandleTransition(user!, WorkDayState.SickDay, commandHandler, cancellationToken),
        "/vacation" => await HandleTransition(user!, WorkDayState.Vacation, commandHandler, cancellationToken),
        "/status" => await HandleStatus(user!, queryHandler, cancellationToken),
        "/help" => GetHelpMessage(),
        _ => "Unknown command. Use /help to see available commands."
      };

      await bot.SendMessage(chatId, response, cancellationToken: cancellationToken);
    }
    catch (InvalidOperationException ex)
    {
      await bot.SendMessage(chatId, $"Error: {ex.Message}", cancellationToken: cancellationToken);
    }
    catch (Exception ex)
    {
      await bot.SendMessage(chatId, $"An error occurred: {ex.Message}", cancellationToken: cancellationToken);
    }
  }

  private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
  {
    Console.WriteLine($"Error: {exception}");
    return Task.CompletedTask;
  }

  private async Task<string> HandleStart(long telegramUserId, string? firstName, IUserRepository userRepo, CancellationToken cancellationToken)
  {
    var user = await userRepo.GetByExternalIdAsync(telegramUserId, cancellationToken);
    if (user != null)
    {
      return $"Welcome back, {user.Name}! Use /help to see available commands.";
    }

    // Create new user
    var newUser = DomainUser.Create(firstName ?? "User", 0);
    newUser.AddExternalIdentity(IdentityProvider.Telegram, telegramUserId);
    await userRepo.AddAsync(newUser, cancellationToken);
    await userRepo.SaveChangesAsync(cancellationToken);

    return $"Welcome, {newUser.Name}! You're now registered. Use /help to see available commands.";
  }

  private async Task<string> HandleTransition(DomainUser user, WorkDayState toState, RecordTransitionCommandHandler handler, CancellationToken cancellationToken)
  {
    var now = DateTime.UtcNow;
    var command = new RecordTransitionCommand(
      user.Id,
      DateOnly.FromDateTime(now),
      toState,
      TimeOnly.FromDateTime(now)
    );

    await handler.HandleAsync(command, cancellationToken);

    var stateMessage = toState switch
    {
      WorkDayState.CommutingToWork => "🚗 Commuting to work",
      WorkDayState.AtWork => "🏢 Arrived at work",
      WorkDayState.Working => "💼 Started working",
      WorkDayState.OnLunch => "🍽️ On lunch break",
      WorkDayState.CommutingHome => "🚗 Commuting home",
      WorkDayState.AtHome => "🏠 At home",
      WorkDayState.SickDay => "🤒 Sick day",
      WorkDayState.Vacation => "🏖️ On vacation",
      _ => $"Transitioned to {toState}"
    };

    return $"{stateMessage} recorded at {now:HH:mm}";
  }

  private async Task<string> HandleStatus(DomainUser user, GetCurrentStatusQueryHandler handler, CancellationToken cancellationToken)
  {
    var query = new GetCurrentStatusQuery(user.Id, DateOnly.FromDateTime(DateTime.UtcNow));
    var result = await handler.HandleAsync(query, cancellationToken);

    var status = $"📊 Current Status: {result.CurrentState}\n\n";
    
    if (result.Transitions.Any())
    {
      status += "Today's transitions:\n";
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
        status += $"{emoji} {transition.ToState} at {transition.Timestamp:HH:mm}\n";
      }
    }
    else
    {
      status += "No transitions recorded today.";
    }

    return status;
  }

  private string GetHelpMessage()
  {
    return """
    📋 TimeSheet Bot Commands:

    /start - Register or login
    /commute - Start commuting to work
    /atwork - Arrive at work (auto-implied when needed)
    /work - Start working
    /lunch - Take lunch break
    /home - Start commuting home
    /done - Arrive home / finish work day
    /emergency - Emergency exit (go home immediately)
    /sickday - Mark as sick day
    /vacation - Mark as vacation
    /status - View today's status
    /help - Show this help message

    💡 Tips:
    - Office work: /commute → /work → /lunch → /work → /home → /done
    - Remote work: /work → /lunch → /work → /done
    
    ℹ️ The system automatically fills in implied transitions.
    For example, /commute then /work will automatically
    record /atwork in between.
    """;
  }
}
