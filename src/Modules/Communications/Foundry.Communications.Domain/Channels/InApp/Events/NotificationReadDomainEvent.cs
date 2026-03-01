using Foundry.Shared.Kernel.Domain;

namespace Foundry.Communications.Domain.Channels.InApp.Events;

public sealed record NotificationReadDomainEvent(
    Guid NotificationId,
    Guid UserId) : DomainEvent;
