# Storage Module - Developer Guide

## Architecture

- **Type:** EF Core (CRUD)
- **Schema:** `storage`
- **Multi-tenant:** Partial (StoredFile is tenant-scoped, StorageBucket is NOT)
- **Events:** None

## Domain Model

**Entities:**
- `StorageBucket` - NOT tenant-scoped, logical grouping with policies
- `StoredFile` - Tenant-scoped file metadata, bytes stored in backend provider

**Value Objects:**
- `RetentionPolicy` - Retention period + action (Archive/Delete/Anonymize)

## Layer Structure

- **Domain:** Bucket, StoredFile entities, RetentionPolicy value object
- **Application:** Commands (CreateBucket, DeleteBucket, UploadFile, DeleteFile), Queries (GetBucketByName, GetFileById, GetFilesByBucket, GetPresignedUrl)
- **Infrastructure:** LocalStorageProvider, S3StorageProvider, StorageDbContext
- **API:** StorageController (file upload/download/deletion)

## Build & Test

```bash
dotnet test tests/Modules/Storage/Storage.Domain.Tests
```

## How to Extend

### Adding a New Storage Provider

1. Implement `IStorageProvider` in Infrastructure/Providers/:
```csharp
public class AzureBlobStorageProvider : IStorageProvider
{
    public Task<string> StoreAsync(string bucket, string key, Stream content, CancellationToken ct);
    public Task<Stream> RetrieveAsync(string bucket, string key, CancellationToken ct);
    public Task DeleteAsync(string bucket, string key, CancellationToken ct);
}
```

2. Register in `StorageModuleExtensions.cs`:
```csharp
services.AddSingleton<IStorageProvider, AzureBlobStorageProvider>();
```

### Adding a New Retention Action

1. Update `RetentionAction` enum in Domain
2. Implement logic in `RetentionService` (Infrastructure)
3. Create background job to enforce policies

## Configuration

```json
{
  "Storage": {
    "Provider": "Local|S3",
    "Local": {
      "BasePath": "/var/foundry/storage"
    },
    "S3": {
      "BucketName": "foundry-storage",
      "Region": "us-east-1",
      "AccessKey": "...",
      "SecretKey": "..."
    }
  }
}
```

## Known Issues

- **CRITICAL:** StorageBucket is NOT tenant-scoped (all tenants share buckets)
- Retention policies defined but no enforcement mechanism (no background job)
- No virus scanning, no encryption at rest
- Versioning flag exists but not implemented
- No domain events published
