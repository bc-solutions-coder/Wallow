using Foundry.Communications.Domain.Preferences;

namespace Foundry.Communications.Application.Channels.Preferences.Commands.SetChannelEnabled;

public sealed record SetChannelEnabledCommand(
    Guid UserId,
    ChannelType ChannelType,
    bool IsEnabled,
    string NotificationType = "*");
