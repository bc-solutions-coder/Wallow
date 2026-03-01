using Foundry.Shared.Contracts.Storage;
using Foundry.Shared.Kernel.Results;
using Foundry.Storage.Application.Interfaces;
using Foundry.Storage.Domain.Entities;
using Foundry.Storage.Domain.Identity;

namespace Foundry.Storage.Application.Commands.DeleteFile;

public sealed class DeleteFileHandler(
    IStoredFileRepository fileRepository,
    IStorageProvider storageProvider)
{
    public async Task<Result> Handle(
        DeleteFileCommand command,
        CancellationToken cancellationToken)
    {
        StoredFileId fileId = StoredFileId.Create(command.FileId);
        StoredFile? file = await fileRepository.GetByIdAsync(fileId, cancellationToken);

        if (file is null)
        {
            return Result.Failure(Error.NotFound("File", command.FileId));
        }

        if (file.TenantId.Value != command.TenantId)
        {
            return Result.Failure(Error.NotFound("File", command.FileId));
        }

        await storageProvider.DeleteAsync(file.StorageKey, cancellationToken);

        fileRepository.Remove(file);
        await fileRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
