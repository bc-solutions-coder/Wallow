namespace Foundry.Billing.Application.DTOs;

public sealed record SubscriptionDto(
    Guid Id,
    Guid UserId,
    string PlanName,
    decimal Price,
    string Currency,
    string Status,
    DateTime StartDate,
    DateTime? EndDate,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd,
    DateTime? CancelledAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Dictionary<string, object>? CustomFields);
