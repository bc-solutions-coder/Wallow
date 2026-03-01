using Foundry.Shared.Kernel.Domain;

namespace Foundry.Billing.Domain.Events;

public sealed record InvoiceOverdueDomainEvent(
    Guid InvoiceId,
    Guid UserId,
    DateTime DueDate) : DomainEvent;
