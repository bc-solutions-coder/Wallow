using Serilog;

namespace Foundry.Tests.Common.Helpers;

public static class SerilogTestHelper
{
    /// <summary>
    /// Resets Serilog's global logger to avoid "logger is already frozen" errors
    /// when multiple test classes create their own WebApplicationFactory.
    /// </summary>
    public static async Task ResetLoggerAsync()
    {
        await Log.CloseAndFlushAsync();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .WriteTo.Console()
            .CreateLogger();
    }

    /// <summary>
    /// Synchronous variant for use in static constructors where async is not available.
    /// </summary>
    public static void ResetLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .WriteTo.Console()
            .CreateLogger();
    }
}
