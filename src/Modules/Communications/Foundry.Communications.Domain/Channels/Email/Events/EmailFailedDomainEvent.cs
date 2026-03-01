using Foundry.Shared.Kernel.Domain;

namespace Foundry.Communications.Domain.Channels.Email.Events;

public sealed record EmailFailedDomainEvent(
    Guid EmailMessageId,
    string ToAddress,
    string FailureReason,
    int RetryCount) : DomainEvent;
