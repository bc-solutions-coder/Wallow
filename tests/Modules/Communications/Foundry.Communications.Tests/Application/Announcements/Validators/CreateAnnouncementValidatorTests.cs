using FluentValidation.TestHelper;
using Foundry.Communications.Application.Announcements.Commands.CreateAnnouncement;
using Foundry.Communications.Domain.Announcements.Enums;

namespace Foundry.Communications.Tests.Application.Announcements.Validators;

public class CreateAnnouncementValidatorTests
{
    private readonly CreateAnnouncementValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Title_Is_Empty()
    {
        CreateAnnouncementCommand command = new(
            "", "Content", AnnouncementType.Feature, AnnouncementTarget.All,
            null, null, null, false, true, null, null, null);

        TestValidationResult<CreateAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title is required");
    }

    [Fact]
    public void Should_Have_Error_When_Title_Exceeds_MaxLength()
    {
        CreateAnnouncementCommand command = new(
            new string('A', 201), "Content", AnnouncementType.Feature, AnnouncementTarget.All,
            null, null, null, false, true, null, null, null);

        TestValidationResult<CreateAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title must not exceed 200 characters");
    }

    [Fact]
    public void Should_Have_Error_When_Content_Is_Empty()
    {
        CreateAnnouncementCommand command = new(
            "Title", "", AnnouncementType.Feature, AnnouncementTarget.All,
            null, null, null, false, true, null, null, null);

        TestValidationResult<CreateAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Content is required");
    }

    [Fact]
    public void Should_Have_Error_When_Content_Exceeds_MaxLength()
    {
        CreateAnnouncementCommand command = new(
            "Title", new string('A', 5001), AnnouncementType.Feature, AnnouncementTarget.All,
            null, null, null, false, true, null, null, null);

        TestValidationResult<CreateAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Content must not exceed 5000 characters");
    }

    [Fact]
    public void Should_Have_Error_When_Content_Contains_ScriptTags()
    {
        CreateAnnouncementCommand command = new(
            "Title", "<script>alert('xss')</script>", AnnouncementType.Feature, AnnouncementTarget.All,
            null, null, null, false, true, null, null, null);

        TestValidationResult<CreateAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Content must not contain script tags");
    }

    [Fact]
    public void Should_Have_Error_When_Type_Is_Invalid()
    {
        CreateAnnouncementCommand command = new(
            "Title", "Content", (AnnouncementType)999, AnnouncementTarget.All,
            null, null, null, false, true, null, null, null);

        TestValidationResult<CreateAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Type)
            .WithErrorMessage("Invalid announcement type");
    }

    [Fact]
    public void Should_Have_Error_When_Target_Is_Invalid()
    {
        CreateAnnouncementCommand command = new(
            "Title", "Content", AnnouncementType.Feature, (AnnouncementTarget)999,
            null, null, null, false, true, null, null, null);

        TestValidationResult<CreateAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Target)
            .WithErrorMessage("Invalid announcement target");
    }

    [Fact]
    public void Should_Have_Error_When_ActionUrl_Exceeds_MaxLength()
    {
        CreateAnnouncementCommand command = new(
            "Title", "Content", AnnouncementType.Feature, AnnouncementTarget.All,
            null, null, null, false, true, new string('A', 2001), null, null);

        TestValidationResult<CreateAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ActionUrl)
            .WithErrorMessage("Action URL must not exceed 2000 characters");
    }

    [Fact]
    public void Should_Have_Error_When_ActionLabel_Exceeds_MaxLength()
    {
        CreateAnnouncementCommand command = new(
            "Title", "Content", AnnouncementType.Feature, AnnouncementTarget.All,
            null, null, null, false, true, null, new string('A', 101), null);

        TestValidationResult<CreateAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ActionLabel)
            .WithErrorMessage("Action label must not exceed 100 characters");
    }

    [Fact]
    public void Should_Have_Error_When_ImageUrl_Exceeds_MaxLength()
    {
        CreateAnnouncementCommand command = new(
            "Title", "Content", AnnouncementType.Feature, AnnouncementTarget.All,
            null, null, null, false, true, null, null, new string('A', 2001));

        TestValidationResult<CreateAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ImageUrl)
            .WithErrorMessage("Image URL must not exceed 2000 characters");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        CreateAnnouncementCommand command = new(
            "Title", "Content", AnnouncementType.Feature, AnnouncementTarget.All,
            null, null, null, false, true, null, null, null);

        TestValidationResult<CreateAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Optional_Fields_Are_Null()
    {
        CreateAnnouncementCommand command = new(
            "Title", "Content", AnnouncementType.Feature, AnnouncementTarget.All,
            null, null, null, false, true, null, null, null);

        TestValidationResult<CreateAnnouncementCommand> result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.ActionUrl);
        result.ShouldNotHaveValidationErrorFor(x => x.ActionLabel);
        result.ShouldNotHaveValidationErrorFor(x => x.ImageUrl);
    }
}
