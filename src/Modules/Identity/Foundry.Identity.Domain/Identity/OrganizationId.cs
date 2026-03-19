using Foundry.Shared.Kernel.Identity;

namespace Foundry.Identity.Domain.Identity;

public readonly record struct OrganizationId(Guid Value) : IStronglyTypedId<OrganizationId>
{
    public static OrganizationId Create(Guid value) => new(value);
    public static OrganizationId New() => new(Guid.NewGuid());
}
