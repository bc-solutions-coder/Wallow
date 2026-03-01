using Foundry.Shared.Kernel.Identity;

namespace Foundry.Billing.Domain.Identity;

public readonly record struct SubscriptionId(Guid Value) : IStronglyTypedId<SubscriptionId>
{
    public static SubscriptionId Create(Guid value) => new(value);
    public static SubscriptionId New() => new(Guid.NewGuid());
}
