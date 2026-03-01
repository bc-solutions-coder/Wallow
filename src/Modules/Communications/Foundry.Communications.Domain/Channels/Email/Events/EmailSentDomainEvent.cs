using Foundry.Shared.Kernel.Domain;

namespace Foundry.Communications.Domain.Channels.Email.Events;

public sealed record EmailSentDomainEvent(
    Guid EmailMessageId,
    string ToAddress,
    string Subject) : DomainEvent;
