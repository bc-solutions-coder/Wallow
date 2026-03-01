using FluentValidation.TestHelper;
using Foundry.Communications.Application.Channels.Email.Commands.SendEmail;

namespace Foundry.Communications.Tests.Application.Channels.Email.Validators;

public class SendEmailValidatorTests
{
    private readonly SendEmailValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_To_Is_Empty()
    {
        SendEmailCommand command = new("", null, "Subject", "Body");

        TestValidationResult<SendEmailCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.To)
            .WithErrorMessage("Recipient email is required");
    }

    [Fact]
    public void Should_Have_Error_When_To_Is_Invalid_Email()
    {
        SendEmailCommand command = new("not-an-email", null, "Subject", "Body");

        TestValidationResult<SendEmailCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.To)
            .WithErrorMessage("Invalid recipient email format");
    }

    [Fact]
    public void Should_Have_Error_When_From_Is_Invalid_Email()
    {
        SendEmailCommand command = new("to@example.com", "not-an-email", "Subject", "Body");

        TestValidationResult<SendEmailCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.From)
            .WithErrorMessage("Invalid sender email format");
    }

    [Fact]
    public void Should_Not_Have_Error_When_From_Is_Null()
    {
        SendEmailCommand command = new("to@example.com", null, "Subject", "Body");

        TestValidationResult<SendEmailCommand> result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.From);
    }

    [Fact]
    public void Should_Have_Error_When_Subject_Is_Empty()
    {
        SendEmailCommand command = new("to@example.com", null, "", "Body");

        TestValidationResult<SendEmailCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Subject)
            .WithErrorMessage("Subject is required");
    }

    [Fact]
    public void Should_Have_Error_When_Subject_Exceeds_MaxLength()
    {
        SendEmailCommand command = new("to@example.com", null, new string('A', 501), "Body");

        TestValidationResult<SendEmailCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Subject)
            .WithErrorMessage("Subject cannot exceed 500 characters");
    }

    [Fact]
    public void Should_Have_Error_When_Body_Is_Empty()
    {
        SendEmailCommand command = new("to@example.com", null, "Subject", "");

        TestValidationResult<SendEmailCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Body)
            .WithErrorMessage("Body is required");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        SendEmailCommand command = new("to@example.com", "from@example.com", "Subject", "Body");

        TestValidationResult<SendEmailCommand> result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
