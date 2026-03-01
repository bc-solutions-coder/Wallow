// Infrastructure extensions - canonical source for module registration
using Foundry.Billing.Infrastructure.Extensions;
using Foundry.Communications.Infrastructure.Extensions;
using Foundry.Configuration.Infrastructure.Extensions;
using Foundry.Identity.Infrastructure.Extensions;
using Foundry.Shared.Infrastructure.Plugins;
using Foundry.Storage.Infrastructure.Extensions;

namespace Foundry.Api;

/// <summary>
/// Central registry for all Foundry modules.
/// Each module provides AddXxxModule() and InitializeXxxModuleAsync() extension methods.
/// </summary>
internal static class FoundryModules
{
    public static IServiceCollection AddFoundryModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ============================================================================
        // PLATFORM MODULES
        // Core infrastructure services used across all domain modules
        // ============================================================================
        services.AddIdentityModule(configuration);
        services.AddBillingModule(configuration);
        services.AddCommunicationsModule(configuration);
        services.AddStorageModule(configuration);

        // ============================================================================
        // FEATURE MODULES
        // Higher-level application features built on platform and domain modules
        // ============================================================================
        services.AddConfigurationModule(configuration);

        // ============================================================================
        // PLUGIN SYSTEM
        // Extensibility via dynamically loaded plugin assemblies
        // ============================================================================
        services.AddFoundryPlugins(configuration);

        return services;
    }

    public static async Task InitializeFoundryModulesAsync(this WebApplication app)
    {
        // ============================================================================
        // PLATFORM MODULES
        // Core infrastructure services - runs DB migrations
        // ============================================================================
        await app.InitializeIdentityModuleAsync();
        await app.InitializeBillingModuleAsync();
        await app.InitializeCommunicationsModuleAsync();
        await app.InitializeStorageModuleAsync();

        // ============================================================================
        // FEATURE MODULES
        // EF Core modules run migrations
        // ============================================================================
        await app.InitializeConfigurationModuleAsync();

        // ============================================================================
        // PLUGIN SYSTEM
        // Discover and optionally load plugins from configured directory
        // ============================================================================
        await app.InitializeFoundryPluginsAsync();
    }
}
