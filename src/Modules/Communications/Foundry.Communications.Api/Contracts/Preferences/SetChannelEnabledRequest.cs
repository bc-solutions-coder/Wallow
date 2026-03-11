using Foundry.Communications.Domain.Preferences;

namespace Foundry.Communications.Api.Contracts.Preferences;

public sealed record SetChannelEnabledRequest(
    ChannelType ChannelType,
    bool IsEnabled);
