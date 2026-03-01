namespace Foundry.Configuration.Application.FeatureFlags.Commands.CreateOverride;

public sealed record CreateOverrideCommand(
    Guid FlagId,
    Guid? TenantId,
    Guid? UserId,
    bool? IsEnabled,
    string? Variant,
    DateTime? ExpiresAt);
