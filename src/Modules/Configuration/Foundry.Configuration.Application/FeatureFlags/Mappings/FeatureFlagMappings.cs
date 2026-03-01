using Foundry.Configuration.Application.FeatureFlags.DTOs;
using Foundry.Configuration.Domain.Entities;

namespace Foundry.Configuration.Application.FeatureFlags.Mappings;

public static class FeatureFlagMappings
{
    public static FeatureFlagDto ToDto(this FeatureFlag flag) => new(
        flag.Id.Value,
        flag.Key,
        flag.Name,
        flag.Description,
        flag.FlagType,
        flag.DefaultEnabled,
        flag.RolloutPercentage,
        flag.Variants.Count > 0
            ? flag.Variants.Select(v => new VariantWeightDto(v.Name, v.Weight)).ToList()
            : null,
        flag.DefaultVariant,
        flag.CreatedAt,
        flag.UpdatedAt);

    public static FeatureFlagOverrideDto ToDto(this FeatureFlagOverride over) => new(
        over.Id.Value,
        over.FlagId.Value,
        over.TenantId,
        over.UserId,
        over.IsEnabled,
        over.Variant,
        over.ExpiresAt,
        over.CreatedAt);
}
