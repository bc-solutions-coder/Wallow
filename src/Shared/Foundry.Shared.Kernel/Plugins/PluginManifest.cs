namespace Foundry.Shared.Kernel.Plugins;

public record PluginDependency(string Id, string VersionRange);

public record PluginManifest(
    string Id,
    string Name,
    string Version,
    string Description,
    string Author,
    string MinFoundryVersion,
    string EntryAssembly,
    IReadOnlyList<PluginDependency> Dependencies,
    IReadOnlyList<string> RequiredPermissions,
    IReadOnlyList<string> ExportedServices);
