using Foundry.Configuration.Domain.Exceptions;

namespace Foundry.Configuration.Tests.Domain;

public class CustomFieldExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessageAndCode()
    {
        CustomFieldException exception = new CustomFieldException("Something went wrong");

        exception.Message.Should().Be("Something went wrong");
        exception.Code.Should().Be("Configuration.CustomField");
    }

    [Fact]
    public void Constructor_Parameterless_CreatesInstance()
    {
        CustomFieldException exception = new CustomFieldException();

        exception.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        Exception inner = new InvalidOperationException("inner");

        CustomFieldException exception = new CustomFieldException("outer", inner);

        exception.Message.Should().Be("outer");
        exception.InnerException.Should().Be(inner);
    }
}
