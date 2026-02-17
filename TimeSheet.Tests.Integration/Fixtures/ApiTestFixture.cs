using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Infrastructure.Persistence;

namespace TimeSheet.Tests.Integration.Fixtures;

/// <summary>
/// Test fixture that provides a WebApplicationFactory for API integration testing.
/// Uses an in-memory SQLite connection (same provider as production) to avoid EF Core
/// internal service provider conflicts that occur when two providers are loaded simultaneously.
/// The connection is kept open for the fixture's lifetime so the in-memory database persists
/// across DbContext instances.
/// </summary>
public class ApiTestFixture : WebApplicationFactory<Program>, IDisposable
{
    // Keep a single SQLite connection open so the :memory: database is not lost between scopes.
    private readonly SqliteConnection _connection;

    public ApiTestFixture()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        // Schema is created lazily on first use via EnsureCreated in InitializeDatabase.
        // Tests that need a clean state call ClearDatabase(), which also ensures the schema exists.
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Connection string is not used directly because we override the DbContext below,
                // but DatabaseOptions validation still requires a non-empty value.
                ["Database:ConnectionString"] = "Data Source=:memory:",
                ["Database:EnableSensitiveDataLogging"] = "true",
                ["Database:EnableDetailedErrors"] = "true",
                ["JwtSettings:SecretKey"] = "test-secret-key-for-integration-tests-at-least-32-characters-long",
                ["JwtSettings:Issuer"] = "TimeSheet.API.Test",
                ["JwtSettings:Audience"] = "TimeSheet.Web.Test",
                ["JwtSettings:ExpirationMinutes"] = "60",
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove the SQLite DbContext registration added by AddPersistenceServices.
            // We replace it with a SQLite in-memory DbContext that shares a single open
            // connection, so the database is not dropped between scoped DbContext instances.
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            // Use the same SQLite provider (no two-provider conflict) but with the shared
            // in-memory connection instead of a file-based or separate :memory: connection.
            var connection = _connection;
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseSqlite(connection);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // Register mock INotificationService (required by ForgotShutdownService)
            var mockNotificationService = new Mock<INotificationService>();
            mockNotificationService
                .Setup(x => x.SendLunchReminderAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockNotificationService
                .Setup(x => x.SendWorkHoursCompleteAsync(It.IsAny<long>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockNotificationService
                .Setup(x => x.SendForgotShutdownReminderAsync(It.IsAny<long>(), It.IsAny<TrackingState>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockNotificationService
                .Setup(x => x.SendAutoShutdownNotificationAsync(It.IsAny<long>(), It.IsAny<TrackingState>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            services.AddSingleton(mockNotificationService.Object);
        });
    }

    /// <summary>
    /// Initializes the database schema. Call once before running tests.
    /// </summary>
    public void InitializeDatabase()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.EnsureCreated();
    }

    /// <summary>
    /// Clears all data from the database between tests.
    /// Also ensures the schema is created if it does not exist yet.
    /// </summary>
    public void ClearDatabase()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure schema exists (idempotent — no-op if already created).
        dbContext.Database.EnsureCreated();

        dbContext.Set<TimeSheet.Core.Domain.Entities.TrackingSession>().RemoveRange(
            dbContext.Set<TimeSheet.Core.Domain.Entities.TrackingSession>());
        dbContext.Set<TimeSheet.Core.Domain.Entities.User>().RemoveRange(
            dbContext.Set<TimeSheet.Core.Domain.Entities.User>());
        dbContext.Set<TimeSheet.Core.Domain.Entities.PendingMnemonic>().RemoveRange(
            dbContext.Set<TimeSheet.Core.Domain.Entities.PendingMnemonic>());

        dbContext.SaveChanges();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }
        base.Dispose(disposing);
    }
}
