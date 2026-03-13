using System.Diagnostics;
using Foundry.Shared.Infrastructure.Core.Middleware;
using Foundry.Shared.Kernel.Domain;
using Wolverine;

namespace Foundry.Shared.Infrastructure.Tests.Middleware;

public class WolverineModuleTaggingMiddlewareAdditionalTests
{
    [Fact]
    public void After_WithValidEnvelope_DoesNotThrow()
    {
        Envelope envelope = new(new FakeModuleMessage());

        Action act = () => WolverineModuleTaggingMiddleware.After(envelope);

        act.Should().NotThrow();
    }

    [Fact]
    public void After_WithNullMessage_DoesNotThrow()
    {
        Envelope envelope = new() { Message = null };

        Action act = () => WolverineModuleTaggingMiddleware.After(envelope);

        act.Should().NotThrow();
    }

    [Fact]
    public void After_WithStartTimestampSet_RecordsMetricsWithoutThrowing()
    {
        Envelope envelope = new(new FakeModuleMessage());
        WolverineModuleTaggingMiddleware.Before(envelope);

        Action act = () => WolverineModuleTaggingMiddleware.After(envelope);

        act.Should().NotThrow();
    }

    [Fact]
    public void Before_WithTenantIdHeader_SetsActivityTag()
    {
        using Activity activity = new("test");
        activity.Start();
        Activity.Current = activity;
        Envelope envelope = new(new FakeModuleMessage());
        envelope.Headers["X-Tenant-Id"] = "tenant-abc";

        WolverineModuleTaggingMiddleware.Before(envelope);

        activity.GetTagItem("foundry.tenant_id").Should().Be("tenant-abc");
    }

    [Fact]
    public void Before_WithEmptyTenantIdHeader_DoesNotSetTag()
    {
        using Activity activity = new("test");
        activity.Start();
        Activity.Current = activity;
        Envelope envelope = new(new FakeModuleMessage());
        envelope.Headers["X-Tenant-Id"] = string.Empty;

        WolverineModuleTaggingMiddleware.Before(envelope);

        activity.GetTagItem("foundry.tenant_id").Should().BeNull();
    }

    [Fact]
    public void Before_WithDomainEvent_DoesNotThrow()
    {
        Activity.Current = null;
        Envelope envelope = new(new FakeDomainEvent());

        Action act = () => WolverineModuleTaggingMiddleware.Before(envelope);

        act.Should().NotThrow();
    }

    [Fact]
    public void Before_SetsStartTimestampHeader()
    {
        Envelope envelope = new(new FakeModuleMessage());

        WolverineModuleTaggingMiddleware.Before(envelope);

        envelope.Headers.TryGetValue("foundry.messaging.start_timestamp", out string? value).Should().BeTrue();
        long.TryParse(value, out _).Should().BeTrue();
    }

    private sealed class FakeModuleMessage;

    private sealed record FakeDomainEvent : DomainEvent;
}
