using System.Diagnostics;
using System.Diagnostics.Metrics;
using Foundry.Shared.Kernel;

namespace Foundry.Identity.Application.Telemetry;

public static class IdentityModuleTelemetry
{
    public static readonly ActivitySource ActivitySource = Diagnostics.CreateActivitySource("Identity");
    public static readonly Meter Meter = Diagnostics.CreateMeter("Identity");

    public static readonly Counter<long> SsoLoginsTotal =
        Meter.CreateCounter<long>("foundry.identity.sso_logins_total");

    public static readonly Counter<long> SsoFailuresTotal =
        Meter.CreateCounter<long>("foundry.identity.sso_failures_total");
}
