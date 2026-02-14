using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Application.Options;
using TimeSheet.Core.Domain.Interfaces;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Infrastructure.Persistence.Repositories;

namespace TimeSheet.Infrastructure.Persistence.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions();
        services.AddDatabase();
        services.AddRepositories();

        return services;
    }

    private static void AddOptions(this IServiceCollection services)
    {
        services.AddValidatedConfiguration<DatabaseOptions>();
    }

    private static void AddDatabase(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            options.UseSqlite(dbOptions.ConnectionString);

            if (dbOptions.EnableSensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging();
            }

            if (dbOptions.EnableDetailedErrors)
            {
                options.EnableDetailedErrors();
            }
        });
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        // Register generic repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Register specialized repositories
        services.AddScoped<ITrackingSessionRepository, TrackingSessionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    private static IServiceCollection AddValidatedConfiguration<T>(this IServiceCollection services)
        where T : class, IOptionsWithSectionName
    {
        services.AddOptions<T>()
            .BindConfiguration(T.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
