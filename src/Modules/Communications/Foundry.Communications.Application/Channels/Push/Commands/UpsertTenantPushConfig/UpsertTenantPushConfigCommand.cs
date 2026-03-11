using Foundry.Communications.Domain.Channels.Push.Enums;
using Foundry.Shared.Kernel.Identity;

namespace Foundry.Communications.Application.Channels.Push.Commands.UpsertTenantPushConfig;

public sealed record UpsertTenantPushConfigCommand(
    TenantId TenantId,
    PushPlatform Platform,
    string RawCredentials);
