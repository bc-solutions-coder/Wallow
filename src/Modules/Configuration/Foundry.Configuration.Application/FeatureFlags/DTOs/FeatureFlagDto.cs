using Foundry.Configuration.Domain.Enums;

namespace Foundry.Configuration.Application.FeatureFlags.DTOs;

public sealed record FeatureFlagDto(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    FlagType FlagType,
    bool DefaultEnabled,
    int? RolloutPercentage,
    IReadOnlyList<VariantWeightDto>? Variants,
    string? DefaultVariant,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
