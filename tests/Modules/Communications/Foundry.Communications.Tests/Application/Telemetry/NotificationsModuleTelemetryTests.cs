using Foundry.Communications.Application.Channels.InApp.Telemetry;

namespace Foundry.Communications.Tests.Application.Telemetry;

public class NotificationsModuleTelemetryTests
{
    [Fact]
    public void ActivitySource_HasCorrectName()
    {
        NotificationsModuleTelemetry.ActivitySource.Name.Should().Be("Foundry.Communications.InApp");
    }

    [Fact]
    public void Meter_HasCorrectName()
    {
        NotificationsModuleTelemetry.Meter.Name.Should().Be("Foundry.Communications.InApp");
    }

    [Fact]
    public void ActivitySource_IsNotNull()
    {
        NotificationsModuleTelemetry.ActivitySource.Should().NotBeNull();
    }

    [Fact]
    public void Meter_IsNotNull()
    {
        NotificationsModuleTelemetry.Meter.Should().NotBeNull();
    }

    [Fact]
    public void ActivitySource_IsSameInstanceOnMultipleAccess()
    {
        System.Diagnostics.ActivitySource first = NotificationsModuleTelemetry.ActivitySource;
        System.Diagnostics.ActivitySource second = NotificationsModuleTelemetry.ActivitySource;

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void Meter_IsSameInstanceOnMultipleAccess()
    {
        System.Diagnostics.Metrics.Meter first = NotificationsModuleTelemetry.Meter;
        System.Diagnostics.Metrics.Meter second = NotificationsModuleTelemetry.Meter;

        first.Should().BeSameAs(second);
    }
}
