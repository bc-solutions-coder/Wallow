using Foundry.Communications.Api.Contracts.Email.Enums;

namespace Foundry.Communications.Api.Contracts.Email.Responses;

public sealed record EmailPreferenceResponse(
    Guid Id,
    Guid UserId,
    ApiNotificationType NotificationType,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
