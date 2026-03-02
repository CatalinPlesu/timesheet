using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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

    // Configure OpenTelemetry tracing
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService(
            builder.Configuration["OpenTelemetry:ServiceName"] ?? "timesheet-bot"))
        .WithTracing(tracing => tracing
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"]
                    ?? "http://localhost:5080/api/default/v1/traces");
                o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                o.Headers = builder.Configuration["OpenTelemetry:Headers"] ?? "";
            }));

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
