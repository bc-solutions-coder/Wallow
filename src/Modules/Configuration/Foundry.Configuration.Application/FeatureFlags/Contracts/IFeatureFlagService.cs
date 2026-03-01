namespace Foundry.Configuration.Application.FeatureFlags.Contracts;

public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string key, Guid tenantId, Guid? userId = null, CancellationToken ct = default);

    Task<string?> GetVariantAsync(string key, Guid tenantId, Guid? userId = null, CancellationToken ct = default);

    Task<Dictionary<string, object>> GetAllFlagsAsync(Guid tenantId, Guid? userId = null, CancellationToken ct = default);
}
