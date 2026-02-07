using Microsoft.Extensions.DependencyInjection;
using TimeSheet.Core.Application.Parsers;

namespace TimeSheet.Core.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Parsers
        services.AddSingleton<ICommandParameterParser, CommandParameterParser>();

        return services;
    }
}
