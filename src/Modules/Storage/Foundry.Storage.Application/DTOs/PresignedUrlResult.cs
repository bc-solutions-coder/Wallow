namespace Foundry.Storage.Application.DTOs;

public sealed record PresignedUrlResult(
    string Url,
    DateTime ExpiresAt);

public sealed record PresignedUploadResult(
    string UploadUrl,
    string StorageKey,
    DateTime ExpiresAt);
