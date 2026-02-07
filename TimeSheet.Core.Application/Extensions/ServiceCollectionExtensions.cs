using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Application.Parsers;
using TimeSheet.Core.Application.Services;
using TimeSheet.Core.Domain.Services;

namespace TimeSheet.Core.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Domain services
        services.AddScoped<ITrackingStateMachine, TrackingStateMachine>();

        // Application services
        services.AddScoped<ITimeTrackingService, TimeTrackingService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IUserSettingsService, UserSettingsService>();
        services.AddScoped<IAutoShutdownService, AutoShutdownService>();
        services.AddScoped<IForgotShutdownService, ForgotShutdownService>();
        services.AddScoped<ReportingService>();
        services.AddSingleton<IMnemonicService, MnemonicService>();

        // Parsers
        services.AddSingleton<ICommandParameterParser, CommandParameterParser>();

        return services;
    }
}
