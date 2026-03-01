using Foundry.Shared.Kernel.Identity;

namespace Foundry.Configuration.Domain.Identity;

public readonly record struct FeatureFlagOverrideId(Guid Value) : IStronglyTypedId<FeatureFlagOverrideId>
{
    public static FeatureFlagOverrideId Create(Guid value) => new(value);
    public static FeatureFlagOverrideId New() => new(Guid.NewGuid());
}
