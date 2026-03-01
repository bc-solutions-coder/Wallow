using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Foundry.Configuration.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddConfigurationApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);
        return services;
    }
}
