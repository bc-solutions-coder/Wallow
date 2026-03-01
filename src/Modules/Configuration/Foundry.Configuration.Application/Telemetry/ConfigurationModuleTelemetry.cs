using System.Diagnostics;
using System.Diagnostics.Metrics;
using Foundry.Shared.Kernel;

namespace Foundry.Configuration.Application.Telemetry;

public static class ConfigurationModuleTelemetry
{
    public static readonly ActivitySource ActivitySource = Diagnostics.CreateActivitySource("Configuration");
    public static readonly Meter Meter = Diagnostics.CreateMeter("Configuration");
}
