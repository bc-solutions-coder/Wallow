using Foundry.Shared.Kernel.Identity;

namespace Foundry.Configuration.Domain.Identity;

public readonly record struct FeatureFlagId(Guid Value) : IStronglyTypedId<FeatureFlagId>
{
    public static FeatureFlagId Create(Guid value) => new(value);
    public static FeatureFlagId New() => new(Guid.NewGuid());
}
