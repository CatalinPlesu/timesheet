using Serilog;
using TimeSheet.Core.Application.Extensions;
using TimeSheet.Infrastructure.Persistence.Extensions;
using TimeSheet.Presentation.Telegram;
using TimeSheet.Presentation.Telegram.Extensions;
using TimeSheet.Presentation.Telegram.Workers;

// Configure Serilog early - before building the host
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TimeSheet Telegram Bot");

    var builder = Host.CreateApplicationBuilder(args);

    // Configure Serilog from appsettings.json
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Register services from all layers (order: Infrastructure -> Application -> Presentation)
    builder.Services.AddPersistenceServices(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddPresentationServices();

    // Register hosted services
    builder.Services.AddHostedService<Worker>();
    builder.Services.AddHostedService<AutoShutdownWorker>();
    builder.Services.AddHostedService<LunchReminderWorker>();
    builder.Services.AddHostedService<WorkHoursAlertWorker>();
    builder.Services.AddHostedService<ForgotShutdownWorker>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
