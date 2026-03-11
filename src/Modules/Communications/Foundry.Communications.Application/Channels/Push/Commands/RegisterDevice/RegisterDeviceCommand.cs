using Foundry.Communications.Domain.Channels.Push.Enums;
using Foundry.Shared.Kernel.Identity;

namespace Foundry.Communications.Application.Channels.Push.Commands.RegisterDevice;

public sealed record RegisterDeviceCommand(
    UserId UserId,
    TenantId TenantId,
    PushPlatform Platform,
    string Token);
