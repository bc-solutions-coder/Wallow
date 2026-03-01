using Foundry.Configuration.Application.FeatureFlags.Contracts;
using Foundry.Configuration.Application.FeatureFlags.DTOs;
using Foundry.Configuration.Application.FeatureFlags.Mappings;
using Foundry.Configuration.Domain.Entities;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Configuration.Application.FeatureFlags.Queries.GetAllFlags;

public sealed class GetAllFlagsHandler(IFeatureFlagRepository repository)
{
    public async Task<Result<IReadOnlyList<FeatureFlagDto>>> Handle(
        GetAllFlagsQuery _,
        CancellationToken ct)
    {
        IReadOnlyList<FeatureFlag> flags = await repository.GetAllAsync(ct);
        List<FeatureFlagDto> dtos = flags.Select(f => f.ToDto()).ToList();
        return Result.Success<IReadOnlyList<FeatureFlagDto>>(dtos);
    }
}
