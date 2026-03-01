namespace Foundry.Shared.Contracts.Storage;

public sealed record UploadResult(
    Guid FileId,
    string FileName,
    string StorageKey,
    long SizeBytes,
    string ContentType,
    DateTime UploadedAt);
