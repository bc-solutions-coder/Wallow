using Foundry.Communications.Application.Channels.Email.Telemetry;

namespace Foundry.Communications.Tests.Application.Telemetry;

public class EmailModuleTelemetryTests
{
    [Fact]
    public void ActivitySource_HasCorrectName()
    {
        EmailModuleTelemetry.ActivitySource.Name.Should().Be("Foundry.Communications.Email");
    }

    [Fact]
    public void Meter_HasCorrectName()
    {
        EmailModuleTelemetry.Meter.Name.Should().Be("Foundry.Communications.Email");
    }

    [Fact]
    public void ActivitySource_IsNotNull()
    {
        EmailModuleTelemetry.ActivitySource.Should().NotBeNull();
    }

    [Fact]
    public void Meter_IsNotNull()
    {
        EmailModuleTelemetry.Meter.Should().NotBeNull();
    }

    [Fact]
    public void ActivitySource_IsSameInstanceOnMultipleAccess()
    {
        System.Diagnostics.ActivitySource first = EmailModuleTelemetry.ActivitySource;
        System.Diagnostics.ActivitySource second = EmailModuleTelemetry.ActivitySource;

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void Meter_IsSameInstanceOnMultipleAccess()
    {
        System.Diagnostics.Metrics.Meter first = EmailModuleTelemetry.Meter;
        System.Diagnostics.Metrics.Meter second = EmailModuleTelemetry.Meter;

        first.Should().BeSameAs(second);
    }
}
