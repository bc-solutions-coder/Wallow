using Foundry.Communications.Domain.Channels.InApp.Entities;
using Foundry.Communications.Domain.Channels.InApp.Enums;
using Foundry.Communications.Domain.Channels.InApp.Events;
using Foundry.Shared.Kernel.Identity;

namespace Foundry.Communications.Tests.Channels.InApp.Domain.Entities;

public class NotificationCreateTests
{
    [Fact]
    public void Create_WithValidData_ReturnsNotificationInUnreadState()
    {
        TenantId tenantId = TenantId.New();
        Guid userId = Guid.NewGuid();
        NotificationType type = NotificationType.SystemAlert;
        string title = "Test Notification";
        string message = "Test message";

        Notification notification = Notification.Create(tenantId, userId, type, title, message);

        notification.TenantId.Should().Be(tenantId);
        notification.UserId.Should().Be(userId);
        notification.Type.Should().Be(type);
        notification.Title.Should().Be(title);
        notification.Message.Should().Be(message);
        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
    }

    [Fact]
    public void Create_RaisesNotificationCreatedEvent()
    {
        Guid userId = Guid.NewGuid();
        string title = "Test";

        Notification notification = Notification.Create(
            TenantId.New(),
            userId,
            NotificationType.SystemAlert,
            title,
            "Message");

        notification.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<NotificationCreatedDomainEvent>()
            .Which.Should().Match<NotificationCreatedDomainEvent>(e =>
                e.UserId == userId && e.Title == title);
    }
}

public class NotificationMarkAsReadTests
{
    [Fact]
    public void MarkAsRead_ChangesIsReadToTrueAndSetsReadAt()
    {
        Notification notification = Notification.Create(
            TenantId.New(),
            Guid.NewGuid(),
            NotificationType.SystemAlert,
            "Test",
            "Message");
        DateTime beforeRead = DateTime.UtcNow;

        notification.MarkAsRead();

        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();
        notification.ReadAt.Should().BeOnOrAfter(beforeRead);
    }

    [Fact]
    public void MarkAsRead_RaisesNotificationReadEvent()
    {
        Guid userId = Guid.NewGuid();
        Notification notification = Notification.Create(
            TenantId.New(),
            userId,
            NotificationType.SystemAlert,
            "Test",
            "Message");

        notification.MarkAsRead();

        notification.DomainEvents.Should().Contain(e => e is NotificationReadDomainEvent);
    }

    [Fact]
    public void MarkAsRead_CalledTwice_UpdatesReadAt()
    {
        Notification notification = Notification.Create(
            TenantId.New(),
            Guid.NewGuid(),
            NotificationType.SystemAlert,
            "Test",
            "Message");
        notification.MarkAsRead();
        DateTime? firstReadAt = notification.ReadAt;

        Thread.Sleep(10);
        notification.MarkAsRead();

        notification.ReadAt.Should().BeAfter(firstReadAt!.Value);
    }
}
