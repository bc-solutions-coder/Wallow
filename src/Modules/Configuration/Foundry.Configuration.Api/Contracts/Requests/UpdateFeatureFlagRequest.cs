namespace Foundry.Configuration.Api.Contracts.Requests;

public sealed record UpdateFeatureFlagRequest(
    string Name,
    string? Description,
    bool DefaultEnabled,
    int? RolloutPercentage);
