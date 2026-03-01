using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Foundry.Shared.Kernel.Plugins;

public interface IFoundryPlugin
{
    PluginManifest Manifest { get; }

    void AddServices(IServiceCollection services, IConfiguration configuration);

    Task InitializeAsync(PluginContext context);

    Task ShutdownAsync();
}
