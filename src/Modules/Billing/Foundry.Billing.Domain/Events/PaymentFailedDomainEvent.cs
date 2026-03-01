using Foundry.Shared.Kernel.Domain;

namespace Foundry.Billing.Domain.Events;

public sealed record PaymentFailedDomainEvent(
    Guid PaymentId,
    Guid InvoiceId,
    string Reason,
    Guid UserId) : DomainEvent;
