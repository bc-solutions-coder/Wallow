using Foundry.Communications.Application.Announcements.Interfaces;
using Foundry.Communications.Domain.Announcements.Entities;
using Foundry.Communications.Domain.Announcements.Identity;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Communications.Application.Announcements.Commands.ArchiveAnnouncement;

public sealed record ArchiveAnnouncementCommand(Guid Id);

public sealed class ArchiveAnnouncementHandler(IAnnouncementRepository repository)
{
    public async Task<Result> Handle(ArchiveAnnouncementCommand command, CancellationToken ct)
    {
        Announcement? announcement = await repository.GetByIdAsync(AnnouncementId.Create(command.Id), ct);
        if (announcement is null)
        {
            return Result.Failure(Error.NotFound("Announcement.NotFound", "Announcement not found"));
        }

        announcement.Archive();
        await repository.UpdateAsync(announcement, ct);

        return Result.Success();
    }
}
