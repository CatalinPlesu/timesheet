using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using TimeSheet.Core.Application.Extensions;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Core.Domain.Interfaces;
using TimeSheet.Infrastructure.Persistence;
using TimeSheet.Infrastructure.Persistence.Extensions;
using TimeSheet.Infrastructure.Persistence.Repositories;
using TimeSheet.Presentation.Telegram.Extensions;
using TimeSheet.Presentation.Telegram.Handlers;
using TimeSheet.Tests.Integration.Mocks;

namespace TimeSheet.Tests.Integration.Fixtures;

/// <summary>
/// Test fixture that provides a complete DI container for integration testing.
/// Sets up real services, in-memory SQLite database, and mock ITelegramBotClient.
/// Implements IDisposable to clean up resources after tests.
/// </summary>
public class TelegramBotTestFixture : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly MockTelegramBotClient _mockBotClient;

    public TelegramBotTestFixture()
    {
        // Build configuration (empty for tests, but needed by DI extensions)
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Add minimal configuration to satisfy validators
                ["Database:ConnectionString"] = "Data Source=:memory:",
                ["Database:EnableSensitiveDataLogging"] = "true",
                ["Database:EnableDetailedErrors"] = "true",
                ["Bot:Token"] = "test-token-not-used",
                ["FrontendUrl"] = "http://localhost:5173"
            })
            .Build();

        // Create mock bot client
        _mockBotClient = new MockTelegramBotClient();

        // Build service collection
        var services = new ServiceCollection();

        // Register IConfiguration as singleton (required by extension methods)
        services.AddSingleton<IConfiguration>(configuration);

        // Add logging (visible in test output)
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Add DbContext with InMemory provider (not SQLite)
        // Use a fixed database name so all scopes share the same in-memory database
        var databaseName = $"TestDb_{Guid.NewGuid()}";
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseInMemoryDatabase(databaseName);
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        // Manually register repositories and Unit of Work (without AddPersistenceServices to avoid SQLite)
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ITrackingSessionRepository, TrackingSessionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPendingMnemonicRepository, PendingMnemonicRepository>();
        services.AddScoped<IHolidayRepository, HolidayRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Add application layer services (domain services, app services, parsers)
        services.AddApplicationServices();

        // Add presentation layer services but override ITelegramBotClient with our mock
        services.AddPresentationServices();
        services.Remove(services.Single(d => d.ServiceType == typeof(ITelegramBotClient)));
        services.AddSingleton(_mockBotClient.Client);

        _serviceProvider = services.BuildServiceProvider();

        // Ensure database is created
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.EnsureCreated();
    }

    /// <summary>
    /// Gets the mock Telegram bot client to inspect captured responses.
    /// </summary>
    public MockTelegramBotClient MockBotClient => _mockBotClient;

    /// <summary>
    /// Gets the service provider for accessing services directly.
    /// </summary>
    public IServiceProvider ServiceProvider => _serviceProvider;

    /// <summary>
    /// Creates a new service scope for a test.
    /// Use this to get scoped services like UpdateHandler, DbContext, etc.
    /// </summary>
    public IServiceScope CreateScope() => _serviceProvider.CreateScope();

    /// <summary>
    /// Gets the UpdateHandler to test bot message processing.
    /// </summary>
    public UpdateHandler GetUpdateHandler()
    {
        var scope = CreateScope();
        return scope.ServiceProvider.GetRequiredService<UpdateHandler>();
    }

    /// <summary>
    /// Gets the AppDbContext to set up test data or verify database state.
    /// </summary>
    public AppDbContext GetDbContext()
    {
        var scope = CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}
