using Foundry.Shared.Kernel.Results;
using Foundry.Storage.Application.DTOs;
using Foundry.Storage.Application.Interfaces;
using Foundry.Storage.Application.Mappings;
using Foundry.Storage.Domain.Entities;

namespace Foundry.Storage.Application.Queries.GetBucketByName;

public sealed class GetBucketByNameHandler(IStorageBucketRepository bucketRepository)
{
    public async Task<Result<BucketDto>> Handle(
        GetBucketByNameQuery query,
        CancellationToken cancellationToken)
    {
        StorageBucket? bucket = await bucketRepository.GetByNameAsync(query.Name, cancellationToken);

        if (bucket is null)
        {
            return Result.Failure<BucketDto>(Error.NotFound("Bucket", query.Name));
        }

        return Result.Success(bucket.ToDto());
    }
}
