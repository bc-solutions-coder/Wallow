using Foundry.Shared.Kernel.Domain;

namespace Foundry.Inquiries.Domain.Events;

public sealed record InquiryStatusChangedDomainEvent(
    Guid InquiryId,
    string OldStatus,
    string NewStatus) : DomainEvent;
