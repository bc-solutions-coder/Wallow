using Foundry.Shared.Kernel.Identity;

namespace Foundry.Identity.Domain.Identity;

public readonly record struct ScimConfigurationId(Guid Value) : IStronglyTypedId<ScimConfigurationId>
{
    public static ScimConfigurationId Create(Guid value) => new(value);
    public static ScimConfigurationId New() => new(Guid.NewGuid());
}
