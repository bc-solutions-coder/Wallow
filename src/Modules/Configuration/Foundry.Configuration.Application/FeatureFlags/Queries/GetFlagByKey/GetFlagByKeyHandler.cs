using Foundry.Configuration.Application.FeatureFlags.Contracts;
using Foundry.Configuration.Application.FeatureFlags.DTOs;
using Foundry.Configuration.Application.FeatureFlags.Mappings;
using Foundry.Configuration.Domain.Entities;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Configuration.Application.FeatureFlags.Queries.GetFlagByKey;

public sealed class GetFlagByKeyHandler(IFeatureFlagRepository repository)
{
    public async Task<Result<FeatureFlagDto>> Handle(
        GetFlagByKeyQuery query,
        CancellationToken ct)
    {
        FeatureFlag? flag = await repository.GetByKeyAsync(query.Key, ct);

        if (flag is null)
        {
            return Result.Failure<FeatureFlagDto>(Error.NotFound("FeatureFlag", query.Key));
        }

        return Result.Success(flag.ToDto());
    }
}
