using TimeSheet.Core.Application.Extensions;
using TimeSheet.Infrastructure.Persistence.Extensions;
using TimeSheet.Presentation.Telegram;
using TimeSheet.Presentation.Telegram.Extensions;
using TimeSheet.Presentation.Telegram.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Register services from all layers (order: Infrastructure -> Application -> Presentation)
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddPresentationServices();

// Register hosted services
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<AutoShutdownWorker>();

var host = builder.Build();
host.Run();
