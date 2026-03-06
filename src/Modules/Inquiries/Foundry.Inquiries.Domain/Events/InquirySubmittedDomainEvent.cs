using Foundry.Shared.Kernel.Domain;

namespace Foundry.Inquiries.Domain.Events;

public sealed record InquirySubmittedDomainEvent(
    Guid InquiryId,
    string Name,
    string Email,
    string? Company,
    string ProjectType,
    string BudgetRange,
    string Timeline,
    string Message) : DomainEvent;
