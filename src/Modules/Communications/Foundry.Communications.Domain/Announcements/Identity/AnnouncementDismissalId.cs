using Foundry.Shared.Kernel.Identity;

namespace Foundry.Communications.Domain.Announcements.Identity;

public readonly record struct AnnouncementDismissalId(Guid Value) : IStronglyTypedId<AnnouncementDismissalId>
{
    public static AnnouncementDismissalId Create(Guid value) => new(value);
    public static AnnouncementDismissalId New() => new(Guid.NewGuid());
}
