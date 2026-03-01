namespace Foundry.Configuration.Api.Contracts.Responses;

public sealed record FeatureFlagOverrideResponse(
    Guid Id,
    Guid FlagId,
    Guid? TenantId,
    Guid? UserId,
    bool? IsEnabled,
    string? Variant,
    DateTime? ExpiresAt,
    DateTime CreatedAt);
