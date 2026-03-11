using Foundry.Communications.Domain.Channels.Push.Enums;

namespace Foundry.Communications.Application.Channels.Push.DTOs;

public sealed record TenantPushConfigDto(
    Guid Id,
    Guid TenantId,
    PushPlatform Platform,
    string EncryptedCredentials,
    bool IsEnabled);
