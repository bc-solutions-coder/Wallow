using Foundry.Communications.Domain.Preferences;

namespace Foundry.Communications.Api.Contracts.Preferences;

public sealed record SetNotificationTypeEnabledRequest(
    ChannelType ChannelType,
    string NotificationType,
    bool IsEnabled);
