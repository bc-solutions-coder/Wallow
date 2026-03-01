using Foundry.Api.Hubs;
using Foundry.Api.Services;
using Foundry.Shared.Contracts.Realtime;
using Foundry.Shared.Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;

namespace Foundry.Api.Tests.Services;

public class SignalRRealtimeDispatcherTests
{
    private readonly IHubContext<RealtimeHub> _hubContext = Substitute.For<IHubContext<RealtimeHub>>();
    private readonly IHtmlSanitizationService _sanitizer = Substitute.For<IHtmlSanitizationService>();
    private readonly IClientProxy _clientProxy = Substitute.For<IClientProxy>();
    private readonly SignalRRealtimeDispatcher _sut;

    public SignalRRealtimeDispatcherTests()
    {
        _sanitizer.Sanitize(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        _sut = new SignalRRealtimeDispatcher(_hubContext, _sanitizer, NullLogger<SignalRRealtimeDispatcher>.Instance);
    }

    [Fact]
    public async Task SendToUser_ShouldCallCorrectClientMethod()
    {
        _hubContext.Clients.User("user-1").Returns(_clientProxy);
        RealtimeEnvelope envelope = RealtimeEnvelope.Create("Notifications", "TaskAssigned", new { TaskId = 42 });

        await _sut.SendToUserAsync("user-1", envelope);

        await _clientProxy.Received(1).SendCoreAsync(
            "ReceiveNotifications",
            Arg.Is<object?[]>(args => args.Length == 1 && MatchesEnvelope(args[0], "Notifications", "TaskAssigned")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendToGroup_ShouldCallCorrectClientMethod()
    {
        _hubContext.Clients.Group("team-alpha").Returns(_clientProxy);
        RealtimeEnvelope envelope = RealtimeEnvelope.Create("Billing", "InvoiceCreated", new { TaskId = 7 });

        await _sut.SendToGroupAsync("team-alpha", envelope);

        await _clientProxy.Received(1).SendCoreAsync(
            "ReceiveBilling",
            Arg.Is<object?[]>(args => args.Length == 1 && MatchesEnvelope(args[0], "Billing", "InvoiceCreated")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendToAll_ShouldCallCorrectClientMethod()
    {
        _hubContext.Clients.All.Returns(_clientProxy);
        RealtimeEnvelope envelope = RealtimeEnvelope.Create("Presence", "UserOnline", new { UserId = "user-1" });

        await _sut.SendToAllAsync(envelope);

        await _clientProxy.Received(1).SendCoreAsync(
            "ReceivePresence",
            Arg.Is<object?[]>(args => args.Length == 1 && MatchesEnvelope(args[0], "Presence", "UserOnline")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendToUser_WhenHubContextThrows_ShouldLogAndNotRethrow()
    {
        _hubContext.Clients.User("user-1").Returns(_clientProxy);
        _clientProxy.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Connection lost"));

        RealtimeEnvelope envelope = RealtimeEnvelope.Create("Notifications", "Alert", new { Message = "test" });

        Func<Task> act = () => _sut.SendToUserAsync("user-1", envelope);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendToGroup_WhenHubContextThrows_ShouldLogAndNotRethrow()
    {
        _hubContext.Clients.Group("team-alpha").Returns(_clientProxy);
        _clientProxy.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Connection lost"));

        RealtimeEnvelope envelope = RealtimeEnvelope.Create("Billing", "InvoiceCreated", new { TaskId = 7 });

        Func<Task> act = () => _sut.SendToGroupAsync("team-alpha", envelope);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendToAll_WhenHubContextThrows_ShouldLogAndNotRethrow()
    {
        _hubContext.Clients.All.Returns(_clientProxy);
        _clientProxy.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Connection lost"));

        RealtimeEnvelope envelope = RealtimeEnvelope.Create("Presence", "UserOnline", new { UserId = "user-1" });

        Func<Task> act = () => _sut.SendToAllAsync(envelope);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendToUser_SanitizesStringPayloadFields()
    {
        _hubContext.Clients.User("user-1").Returns(_clientProxy);
        _sanitizer.Sanitize("<script>xss</script>").Returns("sanitized");

        RealtimeEnvelope envelope = RealtimeEnvelope.Create("Notifications", "Alert", new { Message = "<script>xss</script>" });

        await _sut.SendToUserAsync("user-1", envelope);

        _sanitizer.Received().Sanitize("<script>xss</script>");
    }

    [Fact]
    public async Task SendToAll_SanitizesNestedObjectPayload()
    {
        _hubContext.Clients.All.Returns(_clientProxy);
        _sanitizer.Sanitize(Arg.Any<string>()).Returns(callInfo => $"clean:{callInfo.Arg<string>()}");

        RealtimeEnvelope envelope = RealtimeEnvelope.Create("Module", "Type", new
        {
            Outer = new { Inner = "dirty" }
        });

        await _sut.SendToAllAsync(envelope);

        _sanitizer.Received().Sanitize("dirty");
    }

    [Fact]
    public async Task SendToAll_SanitizesArrayPayload()
    {
        _hubContext.Clients.All.Returns(_clientProxy);
        _sanitizer.Sanitize(Arg.Any<string>()).Returns(callInfo => $"clean:{callInfo.Arg<string>()}");

        List<string> items = ["one", "two"];
        RealtimeEnvelope envelope = RealtimeEnvelope.Create("Module", "Type", new
        {
            Items = items
        });

        await _sut.SendToAllAsync(envelope);

        _sanitizer.Received().Sanitize("one");
        _sanitizer.Received().Sanitize("two");
    }

    [Fact]
    public async Task SendToUser_WithNullPayload_DoesNotThrow()
    {
        _hubContext.Clients.User("user-1").Returns(_clientProxy);
        RealtimeEnvelope envelope = RealtimeEnvelope.Create("Module", "Type", null!);

        Func<Task> act = () => _sut.SendToUserAsync("user-1", envelope);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendToAll_SanitizesArrayWithNestedObjects()
    {
        _hubContext.Clients.All.Returns(_clientProxy);
        _sanitizer.Sanitize(Arg.Any<string>()).Returns(callInfo => $"clean:{callInfo.Arg<string>()}");

        RealtimeEnvelope envelope = RealtimeEnvelope.Create("Module", "Type", new
        {
            Items = new object[] { new { Name = "dirty" } }
        });

        await _sut.SendToAllAsync(envelope);

        _sanitizer.Received().Sanitize("dirty");
    }

    [Fact]
    public async Task SendToAll_WithArrayContainingNulls_DoesNotThrow()
    {
        _hubContext.Clients.All.Returns(_clientProxy);
        _sanitizer.Sanitize(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());

        System.Text.Json.Nodes.JsonNode? payload = System.Text.Json.Nodes.JsonNode.Parse("{\"Items\":[null,\"text\"]}");
        RealtimeEnvelope envelope = RealtimeEnvelope.Create("Module", "Type", payload!);

        Func<Task> act = () => _sut.SendToAllAsync(envelope);

        await act.Should().NotThrowAsync();
    }

    private static bool MatchesEnvelope(object? obj, string module, string type)
        => obj is RealtimeEnvelope e && e.Module == module && e.Type == type;
}
