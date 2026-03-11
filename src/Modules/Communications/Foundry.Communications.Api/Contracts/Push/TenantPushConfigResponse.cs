using Foundry.Communications.Domain.Channels.Push.Enums;

namespace Foundry.Communications.Api.Contracts.Push;

public sealed record TenantPushConfigResponse(
    Guid Id,
    Guid TenantId,
    PushPlatform Platform,
    string Credentials,
    bool IsEnabled);
