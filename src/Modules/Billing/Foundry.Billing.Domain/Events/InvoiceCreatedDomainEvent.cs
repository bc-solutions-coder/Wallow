using Foundry.Shared.Kernel.Domain;

namespace Foundry.Billing.Domain.Events;

public sealed record InvoiceCreatedDomainEvent(
    Guid InvoiceId,
    Guid UserId,
    decimal TotalAmount,
    string Currency) : DomainEvent;
