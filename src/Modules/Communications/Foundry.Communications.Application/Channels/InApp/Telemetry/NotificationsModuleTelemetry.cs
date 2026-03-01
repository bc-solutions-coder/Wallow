using System.Diagnostics;
using System.Diagnostics.Metrics;
using Foundry.Shared.Kernel;

namespace Foundry.Communications.Application.Channels.InApp.Telemetry;

public static class NotificationsModuleTelemetry
{
    public static readonly ActivitySource ActivitySource = Diagnostics.CreateActivitySource("Communications.InApp");
    public static readonly Meter Meter = Diagnostics.CreateMeter("Communications.InApp");
}
