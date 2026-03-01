namespace Foundry.Configuration.Application.FeatureFlags.Commands.UpdateFeatureFlag;

public sealed record UpdateFeatureFlagCommand(
    Guid Id,
    string Name,
    string? Description,
    bool DefaultEnabled,
    int? RolloutPercentage);
