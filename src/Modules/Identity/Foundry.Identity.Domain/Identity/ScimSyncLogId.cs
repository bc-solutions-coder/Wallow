using Foundry.Shared.Kernel.Identity;

namespace Foundry.Identity.Domain.Identity;

public readonly record struct ScimSyncLogId(Guid Value) : IStronglyTypedId<ScimSyncLogId>
{
    public static ScimSyncLogId Create(Guid value) => new(value);
    public static ScimSyncLogId New() => new(Guid.NewGuid());
}
