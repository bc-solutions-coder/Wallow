using Foundry.Shared.Kernel.Identity;

namespace Foundry.Identity.Domain.Identity;

public readonly record struct ServiceAccountMetadataId(Guid Value) : IStronglyTypedId<ServiceAccountMetadataId>
{
    public static ServiceAccountMetadataId Create(Guid value) => new(value);
    public static ServiceAccountMetadataId New() => new(Guid.NewGuid());
}
