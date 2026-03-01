using Foundry.Shared.Kernel.Identity;

namespace Foundry.Storage.Domain.Identity;

public readonly record struct StoredFileId(Guid Value) : IStronglyTypedId<StoredFileId>
{
    public static StoredFileId Create(Guid value) => new(value);
    public static StoredFileId New() => new(Guid.NewGuid());
}
