using TimeSheet.Core.Application.Extensions;
using TimeSheet.Infrastructure.Persistence.Extensions;
using TimeSheet.Presentation.Telegram;
using TimeSheet.Presentation.Telegram.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// Register services from all layers (order: Infrastructure -> Application -> Presentation)
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddPresentationServices();

// Register the Worker as a hosted service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
