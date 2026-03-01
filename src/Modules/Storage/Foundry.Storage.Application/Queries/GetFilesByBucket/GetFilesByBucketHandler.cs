using Foundry.Shared.Kernel.Results;
using Foundry.Storage.Application.DTOs;
using Foundry.Storage.Application.Interfaces;
using Foundry.Storage.Application.Mappings;
using Foundry.Storage.Domain.Entities;

namespace Foundry.Storage.Application.Queries.GetFilesByBucket;

public sealed class GetFilesByBucketHandler(
    IStorageBucketRepository bucketRepository,
    IStoredFileRepository fileRepository)
{
    public async Task<Result<IReadOnlyList<StoredFileDto>>> Handle(
        GetFilesByBucketQuery query,
        CancellationToken cancellationToken)
    {
        StorageBucket? bucket = await bucketRepository.GetByNameAsync(query.BucketName, cancellationToken);
        if (bucket is null)
        {
            return Result.Failure<IReadOnlyList<StoredFileDto>>(
                Error.NotFound("Bucket", query.BucketName));
        }

        IReadOnlyList<StoredFile> files = await fileRepository.GetByBucketIdAsync(
            bucket.Id,
            query.PathPrefix,
            cancellationToken);

        List<StoredFileDto> tenantFiles = files
            .Where(f => f.TenantId.Value == query.TenantId)
            .Select(f => f.ToDto())
            .ToList();

        return tenantFiles;
    }
}
