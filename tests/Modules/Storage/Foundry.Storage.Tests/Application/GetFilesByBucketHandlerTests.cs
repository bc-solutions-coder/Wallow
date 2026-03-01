using Foundry.Shared.Kernel.Identity;
using Foundry.Shared.Kernel.Results;
using Foundry.Storage.Application.DTOs;
using Foundry.Storage.Application.Interfaces;
using Foundry.Storage.Application.Queries.GetFilesByBucket;
using Foundry.Storage.Domain.Entities;

namespace Foundry.Storage.Tests.Application;

public class GetFilesByBucketHandlerTests
{
    private readonly IStorageBucketRepository _bucketRepository;
    private readonly IStoredFileRepository _fileRepository;
    private readonly GetFilesByBucketHandler _handler;

    public GetFilesByBucketHandlerTests()
    {
        _bucketRepository = Substitute.For<IStorageBucketRepository>();
        _fileRepository = Substitute.For<IStoredFileRepository>();
        _handler = new GetFilesByBucketHandler(_bucketRepository, _fileRepository);
    }

    [Fact]
    public async Task Handle_WhenBucketNotFound_ReturnsNotFoundFailure()
    {
        GetFilesByBucketQuery query = new(Guid.NewGuid(), "nonexistent");

        _bucketRepository.GetByNameAsync("nonexistent", Arg.Any<CancellationToken>())
            .Returns((StorageBucket?)null);

        Result<IReadOnlyList<StoredFileDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WhenBucketExistsWithFiles_ReturnsOnlyTenantFiles()
    {
        TenantId tenantId = TenantId.New();
        TenantId otherTenantId = TenantId.New();
        StorageBucket bucket = StorageBucket.Create(tenantId, "shared-bucket");

        StoredFile tenantFile = StoredFile.Create(
            tenantId, bucket.Id, "mine.txt", "text/plain", 100, "key1", Guid.NewGuid());
        StoredFile otherFile = StoredFile.Create(
            otherTenantId, bucket.Id, "theirs.txt", "text/plain", 200, "key2", Guid.NewGuid());

        GetFilesByBucketQuery query = new(tenantId.Value, "shared-bucket");

        _bucketRepository.GetByNameAsync("shared-bucket", Arg.Any<CancellationToken>())
            .Returns(bucket);
        _fileRepository.GetByBucketIdAsync(bucket.Id, null, Arg.Any<CancellationToken>())
            .Returns(new List<StoredFile> { tenantFile, otherFile });

        Result<IReadOnlyList<StoredFileDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].FileName.Should().Be("mine.txt");
    }

    [Fact]
    public async Task Handle_WhenBucketExistsButEmpty_ReturnsEmptyList()
    {
        TenantId tenantId = TenantId.New();
        StorageBucket bucket = StorageBucket.Create(tenantId, "empty-bucket");
        GetFilesByBucketQuery query = new(tenantId.Value, "empty-bucket");

        _bucketRepository.GetByNameAsync("empty-bucket", Arg.Any<CancellationToken>())
            .Returns(bucket);
        _fileRepository.GetByBucketIdAsync(bucket.Id, null, Arg.Any<CancellationToken>())
            .Returns(new List<StoredFile>());

        Result<IReadOnlyList<StoredFileDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithPathPrefix_PassesPrefixToRepository()
    {
        TenantId tenantId = TenantId.New();
        StorageBucket bucket = StorageBucket.Create(tenantId, "bucket");
        GetFilesByBucketQuery query = new(tenantId.Value, "bucket", PathPrefix: "documents/2024");

        _bucketRepository.GetByNameAsync("bucket", Arg.Any<CancellationToken>())
            .Returns(bucket);
        _fileRepository.GetByBucketIdAsync(bucket.Id, "documents/2024", Arg.Any<CancellationToken>())
            .Returns(new List<StoredFile>());

        Result<IReadOnlyList<StoredFileDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _fileRepository.Received(1).GetByBucketIdAsync(bucket.Id, "documents/2024", Arg.Any<CancellationToken>());
    }
}
