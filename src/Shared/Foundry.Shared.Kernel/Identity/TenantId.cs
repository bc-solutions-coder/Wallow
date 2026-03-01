namespace Foundry.Shared.Kernel.Identity;

public readonly record struct TenantId(Guid Value) : IStronglyTypedId<TenantId>
{
    public static TenantId Create(Guid value) => new(value);
    public static TenantId New() => new(Guid.NewGuid());
}
