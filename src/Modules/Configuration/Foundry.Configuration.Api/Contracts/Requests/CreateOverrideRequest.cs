namespace Foundry.Configuration.Api.Contracts.Requests;

public sealed record CreateOverrideRequest(
    Guid? TenantId,
    Guid? UserId,
    bool? IsEnabled,
    string? Variant,
    DateTime? ExpiresAt);
