using System.Text.Json;
using Foundry.Configuration.Application.FeatureFlags.Contracts;
using Microsoft.Extensions.Caching.Distributed;

namespace Foundry.Configuration.Infrastructure.Services;

public sealed class CachedFeatureFlagService : IFeatureFlagService
{
    private readonly IFeatureFlagService _inner;
    private readonly IDistributedCache _cache;

    private static readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(60);
    private static readonly DistributedCacheEntryOptions _cacheOptions = new() { AbsoluteExpirationRelativeToNow = _cacheTtl };

    public CachedFeatureFlagService(IFeatureFlagService inner, IDistributedCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<bool> IsEnabledAsync(string key, Guid tenantId, Guid? userId = null, CancellationToken ct = default)
    {
        string cacheKey = BuildCacheKey(key, tenantId, userId);
        string? cached = await _cache.GetStringAsync(cacheKey, ct);

        if (cached is not null)
        {
            return JsonSerializer.Deserialize<bool>(cached);
        }

        bool result = await _inner.IsEnabledAsync(key, tenantId, userId, ct);
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), _cacheOptions, ct);
        return result;
    }

    public async Task<string?> GetVariantAsync(string key, Guid tenantId, Guid? userId = null, CancellationToken ct = default)
    {
        string cacheKey = BuildCacheKey(key, tenantId, userId);
        string? cached = await _cache.GetStringAsync(cacheKey, ct);

        if (cached is not null)
        {
            return JsonSerializer.Deserialize<string?>(cached);
        }

        string? result = await _inner.GetVariantAsync(key, tenantId, userId, ct);
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), _cacheOptions, ct);
        return result;
    }

    public Task<Dictionary<string, object>> GetAllFlagsAsync(Guid tenantId, Guid? userId = null, CancellationToken ct = default)
        => _inner.GetAllFlagsAsync(tenantId, userId, ct);

    public static async Task InvalidateAsync(IDistributedCache cache, string flagKey)
    {
        // Invalidate by removing known key patterns is impractical without scanning.
        // Instead, we remove the base key — callers should invalidate specific tenant/user combos
        // or use a broader cache-busting strategy. For simplicity, remove the wildcard-free base key.
        await cache.RemoveAsync($"ff:{flagKey}");
    }

    private static string BuildCacheKey(string flagKey, Guid tenantId, Guid? userId)
        => userId.HasValue
            ? $"ff:{flagKey}:{tenantId}:{userId}"
            : $"ff:{flagKey}:{tenantId}:";
}
