using Foundry.Shared.Kernel.Identity;

namespace Foundry.Billing.Domain.Metering.Identity;

public readonly record struct UsageRecordId(Guid Value) : IStronglyTypedId<UsageRecordId>
{
    public static UsageRecordId Create(Guid value) => new(value);
    public static UsageRecordId New() => new(Guid.NewGuid());
}
