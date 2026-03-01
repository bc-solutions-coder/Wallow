using Foundry.Shared.Kernel.Results;
using Foundry.Storage.Application.DTOs;
using Foundry.Storage.Application.Interfaces;
using Foundry.Storage.Application.Mappings;
using Foundry.Storage.Domain.Entities;
using Foundry.Storage.Domain.Identity;

namespace Foundry.Storage.Application.Queries.GetFileById;

public sealed class GetFileByIdHandler(IStoredFileRepository fileRepository)
{
    public async Task<Result<StoredFileDto>> Handle(
        GetFileByIdQuery query,
        CancellationToken cancellationToken)
    {
        StoredFileId fileId = StoredFileId.Create(query.FileId);
        StoredFile? file = await fileRepository.GetByIdAsync(fileId, cancellationToken);

        if (file is null)
        {
            return Result.Failure<StoredFileDto>(Error.NotFound("File", query.FileId));
        }

        if (file.TenantId.Value != query.TenantId)
        {
            return Result.Failure<StoredFileDto>(Error.NotFound("File", query.FileId));
        }

        return Result.Success(file.ToDto());
    }
}
