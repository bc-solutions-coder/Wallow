using Foundry.Communications.Domain.Channels.InApp.Enums;
using Foundry.Communications.Domain.Channels.InApp.Events;
using Foundry.Communications.Domain.Channels.InApp.Identity;
using Foundry.Shared.Kernel.Domain;
using Foundry.Shared.Kernel.Identity;
using Foundry.Shared.Kernel.MultiTenancy;

namespace Foundry.Communications.Domain.Channels.InApp.Entities;

public sealed class Notification : AggregateRoot<NotificationId>, ITenantScoped
{
    public TenantId TenantId { get; set; }
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }

    private Notification() { }

    private Notification(
        TenantId tenantId,
        Guid userId,
        NotificationType type,
        string title,
        string message)
        : base(NotificationId.New())
    {
        TenantId = tenantId;
        UserId = userId;
        Type = type;
        Title = title;
        Message = message;
        IsRead = false;
        SetCreated();

        RaiseDomainEvent(new NotificationCreatedDomainEvent(
            Id.Value,
            UserId,
            Title,
            Type.ToString()));
    }

    public static Notification Create(
        TenantId tenantId,
        Guid userId,
        NotificationType type,
        string title,
        string message)
    {
        return new Notification(tenantId, userId, type, title, message);
    }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
        SetUpdated();

        RaiseDomainEvent(new NotificationReadDomainEvent(
            Id.Value,
            UserId));
    }
}
