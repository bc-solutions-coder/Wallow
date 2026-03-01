using Foundry.Shared.Kernel.Domain;

namespace Foundry.Billing.Domain.Events;

public sealed record SubscriptionCreatedDomainEvent(
    Guid SubscriptionId,
    Guid UserId,
    string PlanName,
    decimal Amount,
    string Currency) : DomainEvent;
