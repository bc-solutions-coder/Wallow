namespace Foundry.Shared.Kernel.Tests.Diagnostics;

public class DiagnosticsTests
{
    [Fact]
    public void Meter_IsNotNull_AndHasExpectedName()
    {
        Kernel.Diagnostics.Meter.Should().NotBeNull();
        Kernel.Diagnostics.Meter.Name.Should().Be("Foundry");
    }

    [Fact]
    public void ActivitySource_IsNotNull_AndHasExpectedName()
    {
        Kernel.Diagnostics.ActivitySource.Should().NotBeNull();
        Kernel.Diagnostics.ActivitySource.Name.Should().Be("Foundry");
    }

    [Fact]
    public void CreateMeter_WithModuleName_ReturnsMeterWithPrefixedName()
    {
        using System.Diagnostics.Metrics.Meter meter = Kernel.Diagnostics.CreateMeter("Billing");

        meter.Should().NotBeNull();
        meter.Name.Should().Be("Foundry.Billing");
    }

    [Fact]
    public void CreateActivitySource_WithModuleName_ReturnsActivitySourceWithPrefixedName()
    {
        using System.Diagnostics.ActivitySource source = Kernel.Diagnostics.CreateActivitySource("Billing");

        source.Should().NotBeNull();
        source.Name.Should().Be("Foundry.Billing");
    }
}
