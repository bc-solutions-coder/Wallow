using FluentValidation.TestHelper;
using Foundry.Communications.Application.Announcements.Commands.UpdateAnnouncement;
using Foundry.Communications.Domain.Announcements.Enums;

namespace Foundry.Communications.Tests.Application.Announcements.Validators;

public class UpdateAnnouncementValidatorTests
{
    private readonly UpdateAnnouncementValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        UpdateAnnouncementCommand command = new(
            Guid.Empty, "Title", "Content", AnnouncementType.Feature, AnnouncementTarget.All,
            null, null, null, false, true, null, null, null);

        TestValidationResult<UpdateAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Announcement ID is required");
    }

    [Fact]
    public void Should_Have_Error_When_Title_Is_Empty()
    {
        UpdateAnnouncementCommand command = new(
            Guid.NewGuid(), "", "Content", AnnouncementType.Feature, AnnouncementTarget.All,
            null, null, null, false, true, null, null, null);

        TestValidationResult<UpdateAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title is required");
    }

    [Fact]
    public void Should_Have_Error_When_Content_Contains_ScriptTags()
    {
        UpdateAnnouncementCommand command = new(
            Guid.NewGuid(), "Title", "<SCRIPT>alert('xss')</SCRIPT>",
            AnnouncementType.Feature, AnnouncementTarget.All,
            null, null, null, false, true, null, null, null);

        TestValidationResult<UpdateAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Content must not contain script tags");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        UpdateAnnouncementCommand command = new(
            Guid.NewGuid(), "Title", "Content", AnnouncementType.Feature, AnnouncementTarget.All,
            null, null, null, false, true, null, null, null);

        TestValidationResult<UpdateAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
