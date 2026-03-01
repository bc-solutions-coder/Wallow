using FluentValidation.TestHelper;
using Foundry.Communications.Application.Announcements.Commands.CreateChangelogEntry;

namespace Foundry.Communications.Tests.Application.Announcements.Validators;

public class CreateChangelogEntryValidatorTests
{
    private readonly CreateChangelogEntryValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Title_Is_Empty()
    {
        CreateChangelogEntryCommand command = new("1.0.0", "", "Content", DateTime.UtcNow);

        TestValidationResult<CreateChangelogEntryCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title is required");
    }

    [Fact]
    public void Should_Have_Error_When_Title_Exceeds_MaxLength()
    {
        CreateChangelogEntryCommand command = new("1.0.0", new string('A', 201), "Content", DateTime.UtcNow);

        TestValidationResult<CreateChangelogEntryCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title must not exceed 200 characters");
    }

    [Fact]
    public void Should_Have_Error_When_Content_Is_Empty()
    {
        CreateChangelogEntryCommand command = new("1.0.0", "Title", "", DateTime.UtcNow);

        TestValidationResult<CreateChangelogEntryCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Content is required");
    }

    [Fact]
    public void Should_Have_Error_When_Content_Exceeds_MaxLength()
    {
        CreateChangelogEntryCommand command = new("1.0.0", "Title", new string('A', 10001), DateTime.UtcNow);

        TestValidationResult<CreateChangelogEntryCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Content must not exceed 10000 characters");
    }


    [Fact]
    public void Should_Have_Error_When_Version_Is_Empty()
    {
        CreateChangelogEntryCommand command = new("", "Title", "Content", DateTime.UtcNow);

        TestValidationResult<CreateChangelogEntryCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Version)
            .WithErrorMessage("Version is required");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1.0")]
    [InlineData("v1.0.0")]
    [InlineData("1.0.0.0")]
    public void Should_Have_Error_When_Version_Is_Invalid_Semver(string version)
    {
        CreateChangelogEntryCommand command = new(version, "Title", "Content", DateTime.UtcNow);

        TestValidationResult<CreateChangelogEntryCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Version)
            .WithErrorMessage("Version must be in valid semver format (e.g. 1.0.0)");
    }

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("2.3.4")]
    [InlineData("1.0.0-alpha")]
    [InlineData("1.0.0-beta.1")]
    [InlineData("1.0.0+build.123")]
    public void Should_Not_Have_Error_When_Version_Is_Valid_Semver(string version)
    {
        CreateChangelogEntryCommand command = new(version, "Title", "Content", DateTime.UtcNow);

        TestValidationResult<CreateChangelogEntryCommand> result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Version);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        CreateChangelogEntryCommand command = new("1.0.0", "Title", "Content", DateTime.UtcNow);

        TestValidationResult<CreateChangelogEntryCommand> result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
