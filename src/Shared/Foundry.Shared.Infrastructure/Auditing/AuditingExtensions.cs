using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Foundry.Shared.Infrastructure.Auditing;

public static class AuditingExtensions
{
    public static IServiceCollection AddFoundryAuditing(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AuditDbContext>((_, options) =>
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "audit");
            });
        });

        services.AddSingleton<AuditInterceptor>();

        return services;
    }

    public static async Task InitializeAuditingAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        AuditDbContext db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        await db.Database.MigrateAsync();
    }
}
