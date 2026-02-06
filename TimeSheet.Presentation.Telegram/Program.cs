using TimeSheet.Presentation.Telegram;
using TimeSheet.Presentation.Telegram.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// Register services from all layers (order: Infrastructure -> Application -> Presentation)
// TODO (Task 3dj.6): Add Infrastructure.Persistence DI extension
// builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

// TODO (Task 3dj.6): Add Core.Application DI extension
// builder.Services.AddApplicationServices();

// Register Presentation layer services
builder.Services.AddPresentationServices();

// Register the Worker as a hosted service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
