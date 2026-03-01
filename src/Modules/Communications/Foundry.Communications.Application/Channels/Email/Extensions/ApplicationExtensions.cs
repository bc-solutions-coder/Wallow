using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Foundry.Communications.Application.Channels.Email.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddEmailApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);
        return services;
    }
}
