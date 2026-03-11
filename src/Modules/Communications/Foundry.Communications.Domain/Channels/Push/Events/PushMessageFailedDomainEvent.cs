using Foundry.Communications.Domain.Channels.Push.Identity;
using Foundry.Shared.Kernel.Domain;

namespace Foundry.Communications.Domain.Channels.Push.Events;

public sealed record PushMessageFailedDomainEvent(
    PushMessageId MessageId,
    string Reason) : DomainEvent;
