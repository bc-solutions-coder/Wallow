namespace Foundry.Storage.Application.Queries.GetFilesByBucket;

public sealed record GetFilesByBucketQuery(
    Guid TenantId,
    string BucketName,
    string? PathPrefix = null);
