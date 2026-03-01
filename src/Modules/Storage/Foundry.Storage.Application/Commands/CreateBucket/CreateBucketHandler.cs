using Foundry.Shared.Kernel.MultiTenancy;
using Foundry.Shared.Kernel.Results;
using Foundry.Storage.Application.DTOs;
using Foundry.Storage.Application.Interfaces;
using Foundry.Storage.Application.Mappings;
using Foundry.Storage.Domain.Entities;
using Foundry.Storage.Domain.ValueObjects;

namespace Foundry.Storage.Application.Commands.CreateBucket;

public sealed class CreateBucketHandler(
    IStorageBucketRepository bucketRepository,
    ITenantContext tenantContext)
{
    public async Task<Result<BucketDto>> Handle(
        CreateBucketCommand command,
        CancellationToken cancellationToken)
    {
        bool exists = await bucketRepository.ExistsByNameAsync(command.Name, cancellationToken);
        if (exists)
        {
            return Result.Failure<BucketDto>(
                Error.Conflict($"Bucket '{command.Name}' already exists"));
        }

        RetentionPolicy? retention = null;
        if (command.RetentionDays.HasValue && command.RetentionAction.HasValue)
        {
            retention = new RetentionPolicy(command.RetentionDays.Value, command.RetentionAction.Value);
        }

        StorageBucket bucket = StorageBucket.Create(
            tenantContext.TenantId,
            command.Name,
            command.Description,
            command.Access,
            command.MaxFileSizeBytes,
            command.AllowedContentTypes,
            retention,
            command.Versioning);

        bucketRepository.Add(bucket);
        await bucketRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(bucket.ToDto());
    }
}
