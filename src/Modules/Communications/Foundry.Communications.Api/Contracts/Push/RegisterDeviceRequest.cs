using Foundry.Communications.Domain.Channels.Push.Enums;

namespace Foundry.Communications.Api.Contracts.Push;

public sealed record RegisterDeviceRequest(
    PushPlatform Platform,
    string Token);
