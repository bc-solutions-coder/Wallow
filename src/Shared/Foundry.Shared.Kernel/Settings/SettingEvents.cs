using Foundry.Shared.Kernel.Domain;
using Foundry.Shared.Kernel.Identity;

namespace Foundry.Shared.Kernel.Settings;

public sealed record TenantSettingChangedEvent(TenantId TenantId, string Key, string? ModuleId) : DomainEvent;

public sealed record UserSettingChangedEvent(UserId UserId, TenantId TenantId, string Key, string? ModuleId) : DomainEvent;
