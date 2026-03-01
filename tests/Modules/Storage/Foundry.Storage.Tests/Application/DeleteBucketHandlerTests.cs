using Foundry.Shared.Contracts.Storage;
using Foundry.Shared.Kernel.Identity;
using Foundry.Shared.Kernel.Results;
using Foundry.Storage.Application.Commands.DeleteBucket;
using Foundry.Storage.Application.Interfaces;
using Foundry.Storage.Domain.Entities;

namespace Foundry.Storage.Tests.Application;

public class DeleteBucketHandlerTests
{
    private readonly IStorageBucketRepository _bucketRepository;
    private readonly IStoredFileRepository _fileRepository;
    private readonly IStorageProvider _storageProvider;
    private readonly DeleteBucketHandler _handler;

    public DeleteBucketHandlerTests()
    {
        _bucketRepository = Substitute.For<IStorageBucketRepository>();
        _fileRepository = Substitute.For<IStoredFileRepository>();
        _storageProvider = Substitute.For<IStorageProvider>();
        _handler = new DeleteBucketHandler(_bucketRepository, _fileRepository, _storageProvider);
    }

    [Fact]
    public async Task Handle_WhenBucketNotFound_ReturnsNotFoundFailure()
    {
        DeleteBucketCommand command = new("nonexistent");
        _bucketRepository.GetByNameAsync(command.Name, Arg.Any<CancellationToken>())
            .Returns((StorageBucket?)null);

        Result result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WhenBucketHasFilesAndNotForced_ReturnsValidationFailure()
    {
        StorageBucket bucket = StorageBucket.Create(TenantId.New(), "has-files");
        StoredFile file = StoredFile.Create(
            TenantId.New(), bucket.Id, "test.txt", "text/plain", 100, "key", Guid.NewGuid());
        DeleteBucketCommand command = new("has-files", Force: false);

        _bucketRepository.GetByNameAsync(command.Name, Arg.Any<CancellationToken>())
            .Returns(bucket);
        _fileRepository.GetByBucketIdAsync(bucket.Id, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new List<StoredFile> { file });

        Result result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().StartWith("Validation");
        result.Error.Message.Should().Contain("1 file(s)");
        _bucketRepository.DidNotReceive().Remove(Arg.Any<StorageBucket>());
    }

    [Fact]
    public async Task Handle_WhenBucketHasFilesAndForced_DeletesFilesAndBucket()
    {
        StorageBucket bucket = StorageBucket.Create(TenantId.New(), "force-delete");
        StoredFile file1 = StoredFile.Create(
            TenantId.New(), bucket.Id, "file1.txt", "text/plain", 100, "key1", Guid.NewGuid());
        StoredFile file2 = StoredFile.Create(
            TenantId.New(), bucket.Id, "file2.txt", "text/plain", 200, "key2", Guid.NewGuid());
        DeleteBucketCommand command = new("force-delete", Force: true);

        _bucketRepository.GetByNameAsync(command.Name, Arg.Any<CancellationToken>())
            .Returns(bucket);
        _fileRepository.GetByBucketIdAsync(bucket.Id, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new List<StoredFile> { file1, file2 });

        Result result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _storageProvider.Received(1).DeleteAsync("key1", Arg.Any<CancellationToken>());
        await _storageProvider.Received(1).DeleteAsync("key2", Arg.Any<CancellationToken>());
        _fileRepository.Received(1).Remove(file1);
        _fileRepository.Received(1).Remove(file2);
        _bucketRepository.Received(1).Remove(bucket);
        await _bucketRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenBucketIsEmpty_DeletesBucketWithoutForce()
    {
        StorageBucket bucket = StorageBucket.Create(TenantId.New(), "empty-bucket");
        DeleteBucketCommand command = new("empty-bucket");

        _bucketRepository.GetByNameAsync(command.Name, Arg.Any<CancellationToken>())
            .Returns(bucket);
        _fileRepository.GetByBucketIdAsync(bucket.Id, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new List<StoredFile>());

        Result result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _bucketRepository.Received(1).Remove(bucket);
        await _bucketRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _storageProvider.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenBucketIsEmptyAndForced_DeletesBucket()
    {
        StorageBucket bucket = StorageBucket.Create(TenantId.New(), "empty-forced");
        DeleteBucketCommand command = new("empty-forced", Force: true);

        _bucketRepository.GetByNameAsync(command.Name, Arg.Any<CancellationToken>())
            .Returns(bucket);
        _fileRepository.GetByBucketIdAsync(bucket.Id, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new List<StoredFile>());

        Result result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _bucketRepository.Received(1).Remove(bucket);
    }
}
