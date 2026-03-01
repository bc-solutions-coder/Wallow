using System.Diagnostics;
using System.Diagnostics.Metrics;
using Foundry.Shared.Kernel;

namespace Foundry.Storage.Application.Telemetry;

public static class StorageModuleTelemetry
{
    public static readonly ActivitySource ActivitySource = Diagnostics.CreateActivitySource("Storage");
    public static readonly Meter Meter = Diagnostics.CreateMeter("Storage");
}
