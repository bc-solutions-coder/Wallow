using Foundry.Communications.Api.Contracts.Email.Enums;

namespace Foundry.Communications.Api.Contracts.Email.Requests;

public sealed record UpdateEmailPreferenceRequest(ApiNotificationType NotificationType, bool IsEnabled);
