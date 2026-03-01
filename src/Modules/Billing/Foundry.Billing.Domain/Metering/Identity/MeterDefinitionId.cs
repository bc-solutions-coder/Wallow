using Foundry.Shared.Kernel.Identity;

namespace Foundry.Billing.Domain.Metering.Identity;

public readonly record struct MeterDefinitionId(Guid Value) : IStronglyTypedId<MeterDefinitionId>
{
    public static MeterDefinitionId Create(Guid value) => new(value);
    public static MeterDefinitionId New() => new(Guid.NewGuid());
}
