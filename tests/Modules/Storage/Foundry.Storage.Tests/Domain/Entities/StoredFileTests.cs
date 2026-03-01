using Foundry.Shared.Kernel.Identity;
using Foundry.Storage.Domain.Entities;
using Foundry.Storage.Domain.Identity;

namespace Foundry.Storage.Tests.Domain.Entities;

public class StoredFileCreateTests
{
    [Fact]
    public void Create_WithValidData_SetsAllProperties()
    {
        TenantId tenantId = TenantId.New();
        StorageBucketId bucketId = StorageBucketId.New();
        string fileName = "test-file.pdf";
        string contentType = "application/pdf";
        long sizeBytes = 12345L;
        string storageKey = $"tenant-{tenantId.Value}/invoices/{Guid.NewGuid()}.pdf";
        Guid uploadedBy = Guid.NewGuid();
        string path = "invoices/2024";
        string metadata = """{"category": "invoice"}""";

        StoredFile file = StoredFile.Create(
            tenantId,
            bucketId,
            fileName,
            contentType,
            sizeBytes,
            storageKey,
            uploadedBy,
            path,
            isPublic: true,
            metadata);

        file.Id.Value.Should().NotBeEmpty();
        file.TenantId.Should().Be(tenantId);
        file.BucketId.Should().Be(bucketId);
        file.FileName.Should().Be(fileName);
        file.ContentType.Should().Be(contentType);
        file.SizeBytes.Should().Be(sizeBytes);
        file.StorageKey.Should().Be(storageKey);
        file.Path.Should().Be(path);
        file.IsPublic.Should().BeTrue();
        file.UploadedBy.Should().Be(uploadedBy);
        file.UploadedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        file.Metadata.Should().Be(metadata);
    }

    [Fact]
    public void Create_WithDefaultOptionalParameters_UsesDefaults()
    {
        TenantId tenantId = TenantId.New();
        StorageBucketId bucketId = StorageBucketId.New();
        Guid uploadedBy = Guid.NewGuid();

        StoredFile file = StoredFile.Create(
            tenantId,
            bucketId,
            "test.txt",
            "text/plain",
            100,
            "key/test.txt",
            uploadedBy);

        file.Path.Should().BeNull();
        file.IsPublic.Should().BeFalse();
        file.Metadata.Should().BeNull();
    }

    [Fact]
    public void Create_GeneratesUniqueId()
    {
        StoredFile file1 = CreateTestFile();
        StoredFile file2 = CreateTestFile();

        file1.Id.Should().NotBe(file2.Id);
    }

    [Fact]
    public void Create_SetsUploadedAtToCurrentUtcTime()
    {
        DateTime before = DateTime.UtcNow;

        StoredFile file = CreateTestFile();

        DateTime after = DateTime.UtcNow;
        file.UploadedAt.Should().BeOnOrAfter(before);
        file.UploadedAt.Should().BeOnOrBefore(after);
    }

    private static StoredFile CreateTestFile()
    {
        return StoredFile.Create(
            TenantId.New(),
            StorageBucketId.New(),
            "test.txt",
            "text/plain",
            100,
            "key/test.txt",
            Guid.NewGuid());
    }
}

public class StoredFileUpdateMetadataTests
{
    [Fact]
    public void UpdateMetadata_WithNewValue_ChangesMetadata()
    {
        StoredFile file = CreateTestFile();
        string newMetadata = """{"category": "updated"}""";

        file.UpdateMetadata(newMetadata);

        file.Metadata.Should().Be(newMetadata);
    }

    [Fact]
    public void UpdateMetadata_WithNull_ClearsMetadata()
    {
        StoredFile file = CreateTestFile(metadata: """{"key": "value"}""");

        file.UpdateMetadata(null);

        file.Metadata.Should().BeNull();
    }

    [Fact]
    public void UpdateMetadata_ReplacesExistingMetadata()
    {
        StoredFile file = CreateTestFile(metadata: """{"old": "data"}""");
        string newMetadata = """{"new": "data"}""";

        file.UpdateMetadata(newMetadata);

        file.Metadata.Should().Be(newMetadata);
    }

    private static StoredFile CreateTestFile(string? metadata = null)
    {
        return StoredFile.Create(
            TenantId.New(),
            StorageBucketId.New(),
            "test.txt",
            "text/plain",
            100,
            "key/test.txt",
            Guid.NewGuid(),
            metadata: metadata);
    }
}

public class StoredFileSetPublicTests
{
    [Fact]
    public void SetPublic_WithTrue_SetsIsPublicToTrue()
    {
        StoredFile file = CreateTestFile(isPublic: false);

        file.SetPublic(true);

        file.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void SetPublic_WithFalse_SetsIsPublicToFalse()
    {
        StoredFile file = CreateTestFile(isPublic: true);

        file.SetPublic(false);

        file.IsPublic.Should().BeFalse();
    }

    [Fact]
    public void SetPublic_WithSameValue_RemainsUnchanged()
    {
        StoredFile file = CreateTestFile(isPublic: true);

        file.SetPublic(true);

        file.IsPublic.Should().BeTrue();
    }

    private static StoredFile CreateTestFile(bool isPublic = false)
    {
        return StoredFile.Create(
            TenantId.New(),
            StorageBucketId.New(),
            "test.txt",
            "text/plain",
            100,
            "key/test.txt",
            Guid.NewGuid(),
            isPublic: isPublic);
    }
}
