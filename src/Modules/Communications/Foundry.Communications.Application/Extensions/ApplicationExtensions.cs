using FluentValidation;
using Foundry.Communications.Application.Announcements.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Foundry.Communications.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddCommunicationsApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);
        services.AddScoped<IAnnouncementTargetingService, AnnouncementTargetingService>();
        return services;
    }
}
