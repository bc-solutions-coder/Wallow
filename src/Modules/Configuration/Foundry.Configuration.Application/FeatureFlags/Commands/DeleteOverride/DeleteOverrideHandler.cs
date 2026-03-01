using Foundry.Configuration.Application.FeatureFlags.Contracts;
using Foundry.Configuration.Domain.Entities;
using Foundry.Configuration.Domain.Identity;
using Foundry.Shared.Kernel.Results;
using Microsoft.Extensions.Caching.Distributed;

namespace Foundry.Configuration.Application.FeatureFlags.Commands.DeleteOverride;

public sealed class DeleteOverrideHandler(
    IFeatureFlagOverrideRepository repository,
    IFeatureFlagRepository flagRepo,
    IDistributedCache cache)
{
    public async Task<Result> Handle(DeleteOverrideCommand cmd, CancellationToken ct)
    {
        FeatureFlagOverrideId overrideId = FeatureFlagOverrideId.Create(cmd.Id);
        FeatureFlagOverride? over = await repository.GetByIdAsync(overrideId, ct);

        if (over is null)
        {
            return Result.Failure(Error.NotFound("FeatureFlagOverride", cmd.Id));
        }

        FeatureFlag? flag = await flagRepo.GetByIdAsync(over.FlagId, ct);

        await repository.DeleteAsync(over, ct);

        if (flag is not null)
        {
            await cache.RemoveAsync($"ff:{flag.Key}", ct);
        }

        return Result.Success();
    }
}
