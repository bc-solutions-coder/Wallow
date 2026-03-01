namespace Foundry.Billing.Application.DTOs;

public sealed record PaymentDto(
    Guid Id,
    Guid InvoiceId,
    Guid UserId,
    decimal Amount,
    string Currency,
    string Method,
    string Status,
    string? TransactionReference,
    string? FailureReason,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Dictionary<string, object>? CustomFields);
