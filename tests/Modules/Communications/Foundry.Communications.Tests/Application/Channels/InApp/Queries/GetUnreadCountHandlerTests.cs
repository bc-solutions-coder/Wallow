using Foundry.Communications.Application.Channels.InApp.Interfaces;
using Foundry.Communications.Application.Channels.InApp.Queries.GetUnreadCount;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Communications.Tests.Application.Channels.InApp.Queries;

public class GetUnreadCountHandlerTests
{
    private readonly INotificationRepository _repository;
    private readonly GetUnreadCountHandler _handler;

    public GetUnreadCountHandlerTests()
    {
        _repository = Substitute.For<INotificationRepository>();
        _handler = new GetUnreadCountHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsUnreadCount()
    {
        Guid userId = Guid.NewGuid();
        _repository.GetUnreadCountAsync(userId, Arg.Any<CancellationToken>())
            .Returns(5);

        GetUnreadCountQuery query = new(userId);

        Result<int> result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WhenNoUnread_ReturnsZero()
    {
        Guid userId = Guid.NewGuid();
        _repository.GetUnreadCountAsync(userId, Arg.Any<CancellationToken>())
            .Returns(0);

        GetUnreadCountQuery query = new(userId);

        Result<int> result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        using CancellationTokenSource cts = new();
        Guid userId = Guid.NewGuid();
        _repository.GetUnreadCountAsync(userId, Arg.Any<CancellationToken>())
            .Returns(0);

        GetUnreadCountQuery query = new(userId);

        await _handler.Handle(query, cts.Token);

        await _repository.Received(1).GetUnreadCountAsync(userId, cts.Token);
    }
}
