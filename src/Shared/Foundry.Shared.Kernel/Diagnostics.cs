using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Foundry.Shared.Kernel;

public static class Diagnostics
{
    public static readonly Meter Meter = new("Foundry");
    public static readonly ActivitySource ActivitySource = new("Foundry");

    public static ActivitySource CreateActivitySource(string moduleName) =>
        new($"Foundry.{moduleName}");

    public static Meter CreateMeter(string moduleName) =>
        new($"Foundry.{moduleName}");
}
