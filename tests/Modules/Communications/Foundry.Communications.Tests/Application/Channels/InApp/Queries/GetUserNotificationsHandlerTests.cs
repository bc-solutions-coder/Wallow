using Foundry.Communications.Application.Channels.InApp.DTOs;
using Foundry.Communications.Application.Channels.InApp.Interfaces;
using Foundry.Communications.Application.Channels.InApp.Queries.GetUserNotifications;
using Foundry.Communications.Domain.Channels.InApp.Entities;
using Foundry.Communications.Domain.Channels.InApp.Enums;
using Foundry.Shared.Kernel.Identity;
using Foundry.Shared.Kernel.Pagination;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Communications.Tests.Application.Channels.InApp.Queries;

public class GetUserNotificationsHandlerTests
{
    private readonly INotificationRepository _repository;
    private readonly GetUserNotificationsHandler _handler;

    public GetUserNotificationsHandlerTests()
    {
        _repository = Substitute.For<INotificationRepository>();
        _handler = new GetUserNotificationsHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsPagedNotifications()
    {
        Guid userId = Guid.NewGuid();
        TenantId tenantId = TenantId.Create(Guid.NewGuid());

        List<Notification> notifications = new()
        {
            Notification.Create(tenantId, userId, NotificationType.SystemAlert, "Title 1", "Message 1"),
            Notification.Create(tenantId, userId, NotificationType.TaskAssigned, "Title 2", "Message 2")
        };

        _repository.GetByUserIdPagedAsync(userId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((notifications, 2));

        GetUserNotificationsQuery query = new(userId);

        Result<PagedResult<NotificationDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_WithCustomPagination_PassesCorrectValues()
    {
        Guid userId = Guid.NewGuid();
        _repository.GetByUserIdPagedAsync(userId, 3, 10, Arg.Any<CancellationToken>())
            .Returns((new List<Notification>(), 0));

        GetUserNotificationsQuery query = new(userId, PageNumber: 3, PageSize: 10);

        await _handler.Handle(query, CancellationToken.None);

        await _repository.Received(1).GetByUserIdPagedAsync(userId, 3, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoNotifications_ReturnsEmptyPage()
    {
        Guid userId = Guid.NewGuid();
        _repository.GetByUserIdPagedAsync(userId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<Notification>(), 0));

        GetUserNotificationsQuery query = new(userId);

        Result<PagedResult<NotificationDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_MapsNotificationFieldsCorrectly()
    {
        Guid userId = Guid.NewGuid();
        TenantId tenantId = TenantId.Create(Guid.NewGuid());

        Notification notification = Notification.Create(tenantId, userId, NotificationType.Mention, "Test Title", "Test Message");

        _repository.GetByUserIdPagedAsync(userId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<Notification> { notification }, 1));

        GetUserNotificationsQuery query = new(userId);

        Result<PagedResult<NotificationDto>> result = await _handler.Handle(query, CancellationToken.None);

        NotificationDto dto = result.Value.Items[0];
        dto.UserId.Should().Be(userId);
        dto.Title.Should().Be("Test Title");
        dto.Message.Should().Be("Test Message");
        dto.Type.Should().Be(nameof(NotificationType.Mention));
        dto.IsRead.Should().BeFalse();
    }
}
