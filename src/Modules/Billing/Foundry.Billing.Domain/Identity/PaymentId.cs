using Foundry.Shared.Kernel.Identity;

namespace Foundry.Billing.Domain.Identity;

public readonly record struct PaymentId(Guid Value) : IStronglyTypedId<PaymentId>
{
    public static PaymentId Create(Guid value) => new(value);
    public static PaymentId New() => new(Guid.NewGuid());
}
