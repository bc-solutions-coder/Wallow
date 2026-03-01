using Foundry.Shared.Kernel.Domain;

namespace Foundry.Billing.Domain.Events;

public sealed record InvoicePaidDomainEvent(
    Guid InvoiceId,
    Guid PaymentId,
    DateTime PaidAt) : DomainEvent;
