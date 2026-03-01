using Foundry.Shared.Kernel.Domain;

namespace Foundry.Billing.Domain.Events;

public sealed record PaymentReceivedDomainEvent(
    Guid PaymentId,
    Guid InvoiceId,
    decimal Amount,
    string Currency,
    Guid UserId) : DomainEvent;
