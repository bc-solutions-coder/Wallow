using Foundry.Communications.Domain.Channels.Push.Enums;

namespace Foundry.Communications.Api.Contracts.Push;

public sealed record UpsertTenantPushConfigRequest(
    PushPlatform Platform,
    string Credentials);
