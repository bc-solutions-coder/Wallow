using System.Diagnostics;
using System.Diagnostics.Metrics;
using Foundry.Shared.Kernel;

namespace Foundry.Communications.Application.Channels.Email.Telemetry;

public static class EmailModuleTelemetry
{
    public static readonly ActivitySource ActivitySource = Diagnostics.CreateActivitySource("Communications.Email");
    public static readonly Meter Meter = Diagnostics.CreateMeter("Communications.Email");
}
