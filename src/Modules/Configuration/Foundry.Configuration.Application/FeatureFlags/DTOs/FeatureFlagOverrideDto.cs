namespace Foundry.Configuration.Application.FeatureFlags.DTOs;

public sealed record FeatureFlagOverrideDto(
    Guid Id,
    Guid FlagId,
    Guid? TenantId,
    Guid? UserId,
    bool? IsEnabled,
    string? Variant,
    DateTime? ExpiresAt,
    DateTime CreatedAt);
