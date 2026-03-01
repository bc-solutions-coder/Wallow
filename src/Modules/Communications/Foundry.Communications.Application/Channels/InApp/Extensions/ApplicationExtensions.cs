using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Foundry.Communications.Application.Channels.InApp.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);
        return services;
    }
}
