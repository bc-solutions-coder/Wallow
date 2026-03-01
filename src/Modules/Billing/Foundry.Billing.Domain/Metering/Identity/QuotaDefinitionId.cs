using Foundry.Shared.Kernel.Identity;

namespace Foundry.Billing.Domain.Metering.Identity;

public readonly record struct QuotaDefinitionId(Guid Value) : IStronglyTypedId<QuotaDefinitionId>
{
    public static QuotaDefinitionId Create(Guid value) => new(value);
    public static QuotaDefinitionId New() => new(Guid.NewGuid());
}
