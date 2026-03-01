using Serilog;

namespace Foundry.Api.Jobs;

internal sealed class SystemHeartbeatJob
{
    public Task ExecuteAsync()
    {
        Log.Information("Heartbeat: system alive at {Timestamp}", DateTime.UtcNow);
        return Task.CompletedTask;
    }
}
