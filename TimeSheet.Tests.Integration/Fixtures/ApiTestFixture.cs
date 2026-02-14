using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
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
/// Sets up real services with in-memory database.
/// </summary>
public class ApiTestFixture : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TestApiDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
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
            // Remove all DbContext-related registrations (SQLite provider from AddPersistenceServices)
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            // Also remove the Action<DbContextOptionsBuilder> configurations
            var dbContextOptionsDescriptors = services.Where(d =>
                d.ServiceType.IsGenericType &&
                d.ServiceType.GetGenericTypeDefinition() == typeof(Action<>))
                .ToList();

            foreach (var descriptor in dbContextOptionsDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add InMemory database for testing
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(_databaseName);
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
    /// Initializes the database and ensures it's created.
    /// </summary>
    public void InitializeDatabase()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.EnsureCreated();
    }

    /// <summary>
    /// Clears all data from the database.
    /// </summary>
    public void ClearDatabase()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.Set<TimeSheet.Core.Domain.Entities.TrackingSession>().RemoveRange(
            dbContext.Set<TimeSheet.Core.Domain.Entities.TrackingSession>());
        dbContext.Set<TimeSheet.Core.Domain.Entities.User>().RemoveRange(
            dbContext.Set<TimeSheet.Core.Domain.Entities.User>());

        dbContext.SaveChanges();
    }
}
