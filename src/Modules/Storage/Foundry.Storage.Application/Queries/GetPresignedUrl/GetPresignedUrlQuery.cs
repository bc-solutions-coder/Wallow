namespace Foundry.Storage.Application.Queries.GetPresignedUrl;

public sealed record GetPresignedUrlQuery(
    Guid TenantId,
    Guid FileId,
    TimeSpan? Expiry = null,
    bool ForceDownload = false);
