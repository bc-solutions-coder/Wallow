namespace Foundry.Billing.Application.DTOs;

public sealed record InvoiceDto(
    Guid Id,
    Guid UserId,
    string InvoiceNumber,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime? DueDate,
    DateTime? PaidAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<InvoiceLineItemDto> LineItems,
    Dictionary<string, object>? CustomFields);
