using Foundry.Configuration.Application.FeatureFlags.DTOs;
using Foundry.Configuration.Domain.Enums;

namespace Foundry.Configuration.Application.FeatureFlags.Commands.CreateFeatureFlag;

public sealed record CreateFeatureFlagCommand(
    string Key,
    string Name,
    string? Description,
    FlagType FlagType,
    bool DefaultEnabled,
    int? RolloutPercentage,
    IReadOnlyList<VariantWeightDto>? Variants,
    string? DefaultVariant);
