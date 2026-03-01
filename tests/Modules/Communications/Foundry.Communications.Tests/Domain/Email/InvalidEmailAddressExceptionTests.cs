using Foundry.Communications.Domain.Channels.Email.Exceptions;

namespace Foundry.Communications.Tests.Domain.Email;

public class InvalidEmailAddressExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessageAndCode()
    {
        InvalidEmailAddressException exception = new("Test message");

        exception.Message.Should().Be("Test message");
        exception.Code.Should().Be("Email.InvalidEmailAddress");
    }

    [Fact]
    public void Constructor_Default_CreatesException()
    {
        InvalidEmailAddressException exception = new();

        exception.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsProperties()
    {
        InvalidOperationException inner = new("Inner error");

        InvalidEmailAddressException exception = new("Outer message", inner);

        exception.Message.Should().Be("Outer message");
        exception.InnerException.Should().Be(inner);
    }
}
