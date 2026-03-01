using FluentValidation;

namespace Foundry.Communications.Application.Announcements.Commands.ArchiveAnnouncement;

public sealed class ArchiveAnnouncementValidator : AbstractValidator<ArchiveAnnouncementCommand>
{
    public ArchiveAnnouncementValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Announcement ID is required");
    }
}
