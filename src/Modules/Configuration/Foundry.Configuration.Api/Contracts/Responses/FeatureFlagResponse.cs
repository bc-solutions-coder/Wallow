using Foundry.Configuration.Api.Contracts.Enums;
using Foundry.Configuration.Application.FeatureFlags.DTOs;

namespace Foundry.Configuration.Api.Contracts.Responses;

public sealed record FeatureFlagResponse(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    ApiFlagType FlagType,
    bool DefaultEnabled,
    int? RolloutPercentage,
    IReadOnlyList<VariantWeightDto>? Variants,
    string? DefaultVariant,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
