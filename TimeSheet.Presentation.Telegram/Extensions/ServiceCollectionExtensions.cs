using Microsoft.Extensions.Options;
using Telegram.Bot;
using TimeSheet.Core.Application.Options;
using TimeSheet.Presentation.Telegram.Handlers;
using TimeSheet.Presentation.Telegram.Options;
using TimeSheet.Presentation.Telegram.Services;

namespace TimeSheet.Presentation.Telegram.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        services.AddOptions();
        services.AddTelegramBot();
        services.AddHandlers();
        services.AddPresentationInfrastructure();

        return services;
    }

    private static void AddPresentationInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<RegistrationSessionStore>();
    }

    private static void AddOptions(this IServiceCollection services)
    {
        services.AddValidatedConfiguration<BotOptions>();
    }

    private static void AddTelegramBot(this IServiceCollection services)
    {
        services.AddSingleton<ITelegramBotClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BotOptions>>();
            return new TelegramBotClient(options.Value.Token);
        });
    }

    private static void AddHandlers(this IServiceCollection services)
    {
        services.AddSingleton<UpdateHandler>();
        services.AddSingleton<TrackingCommandHandler>();
        services.AddSingleton<RegistrationCommandHandler>();
        services.AddSingleton<AboutCommandHandler>();
        services.AddSingleton<HelpCommandHandler>();
        services.AddSingleton<EditCommandHandler>();
        services.AddSingleton<DeleteCommandHandler>();
        services.AddSingleton<GenerateCommandHandler>();
        services.AddSingleton<ListCommandHandler>();
        services.AddSingleton<SettingsCommandHandler>();
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
