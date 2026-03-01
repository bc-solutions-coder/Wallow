using FluentValidation.TestHelper;
using Foundry.Communications.Application.Announcements.Commands.ArchiveAnnouncement;
using Foundry.Communications.Application.Announcements.Commands.DismissAnnouncement;
using Foundry.Communications.Application.Announcements.Commands.PublishAnnouncement;
using Foundry.Communications.Application.Announcements.Commands.PublishChangelogEntry;

namespace Foundry.Communications.Tests.Application.Announcements.Validators;

public class ArchiveAnnouncementValidatorTests
{
    private readonly ArchiveAnnouncementValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        ArchiveAnnouncementCommand command = new(Guid.Empty);

        TestValidationResult<ArchiveAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Announcement ID is required");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Id_Is_Valid()
    {
        ArchiveAnnouncementCommand command = new(Guid.NewGuid());

        TestValidationResult<ArchiveAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class DismissAnnouncementValidatorTests
{
    private readonly DismissAnnouncementValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_AnnouncementId_Is_Empty()
    {
        DismissAnnouncementCommand command = new(Guid.Empty, Guid.NewGuid());

        TestValidationResult<DismissAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AnnouncementId)
            .WithErrorMessage("Announcement ID is required");
    }

    [Fact]
    public void Should_Have_Error_When_UserId_Is_Empty()
    {
        DismissAnnouncementCommand command = new(Guid.NewGuid(), Guid.Empty);

        TestValidationResult<DismissAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        DismissAnnouncementCommand command = new(Guid.NewGuid(), Guid.NewGuid());

        TestValidationResult<DismissAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class PublishAnnouncementValidatorTests
{
    private readonly PublishAnnouncementValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        PublishAnnouncementCommand command = new(Guid.Empty);

        TestValidationResult<PublishAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Announcement ID is required");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Id_Is_Valid()
    {
        PublishAnnouncementCommand command = new(Guid.NewGuid());

        TestValidationResult<PublishAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class PublishChangelogEntryValidatorTests
{
    private readonly PublishChangelogEntryValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        PublishChangelogEntryCommand command = new(Guid.Empty);

        TestValidationResult<PublishChangelogEntryCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Changelog entry ID is required");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Id_Is_Valid()
    {
        PublishChangelogEntryCommand command = new(Guid.NewGuid());

        TestValidationResult<PublishChangelogEntryCommand> result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
