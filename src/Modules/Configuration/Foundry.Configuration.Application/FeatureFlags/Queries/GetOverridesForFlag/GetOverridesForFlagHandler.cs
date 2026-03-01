using Foundry.Configuration.Application.FeatureFlags.Contracts;
using Foundry.Configuration.Application.FeatureFlags.DTOs;
using Foundry.Configuration.Application.FeatureFlags.Mappings;
using Foundry.Configuration.Domain.Entities;
using Foundry.Configuration.Domain.Identity;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Configuration.Application.FeatureFlags.Queries.GetOverridesForFlag;

public sealed class GetOverridesForFlagHandler(IFeatureFlagOverrideRepository repository)
{
    public async Task<Result<IReadOnlyList<FeatureFlagOverrideDto>>> Handle(
        GetOverridesForFlagQuery query,
        CancellationToken ct)
    {
        FeatureFlagId flagId = FeatureFlagId.Create(query.FlagId);
        IReadOnlyList<FeatureFlagOverride> overrides = await repository.GetOverridesForFlagAsync(flagId, ct);
        List<FeatureFlagOverrideDto> dtos = overrides.Select(o => o.ToDto()).ToList();
        return Result.Success<IReadOnlyList<FeatureFlagOverrideDto>>(dtos);
    }
}
