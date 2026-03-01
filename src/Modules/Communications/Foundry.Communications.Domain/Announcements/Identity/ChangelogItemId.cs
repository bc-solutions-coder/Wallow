using Foundry.Shared.Kernel.Identity;

namespace Foundry.Communications.Domain.Announcements.Identity;

public readonly record struct ChangelogItemId(Guid Value) : IStronglyTypedId<ChangelogItemId>
{
    public static ChangelogItemId Create(Guid value) => new(value);
    public static ChangelogItemId New() => new(Guid.NewGuid());
}
