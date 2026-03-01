using Foundry.Shared.Kernel.Identity;

namespace Foundry.Billing.Domain.Identity;

public readonly record struct InvoiceId(Guid Value) : IStronglyTypedId<InvoiceId>
{
    public static InvoiceId Create(Guid value) => new(value);
    public static InvoiceId New() => new(Guid.NewGuid());
}
