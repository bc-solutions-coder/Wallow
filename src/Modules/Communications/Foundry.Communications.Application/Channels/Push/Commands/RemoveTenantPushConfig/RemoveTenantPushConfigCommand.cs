using Foundry.Communications.Domain.Channels.Push.Enums;
using Foundry.Shared.Kernel.Identity;

namespace Foundry.Communications.Application.Channels.Push.Commands.RemoveTenantPushConfig;

public sealed record RemoveTenantPushConfigCommand(
    TenantId TenantId,
    PushPlatform Platform);
