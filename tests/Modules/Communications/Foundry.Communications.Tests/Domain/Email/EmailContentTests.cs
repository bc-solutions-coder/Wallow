using Foundry.Communications.Domain.Channels.Email.ValueObjects;

namespace Foundry.Communications.Tests.Domain.Email;

public class EmailContentCreateTests
{
    [Fact]
    public void Create_WithValidData_ReturnsEmailContent()
    {
        EmailContent content = EmailContent.Create("Test Subject", "Test Body");

        content.Subject.Should().Be("Test Subject");
        content.Body.Should().Be("Test Body");
    }

    [Fact]
    public void Create_TrimsSubjectAndBody()
    {
        EmailContent content = EmailContent.Create("  Subject  ", "  Body  ");

        content.Subject.Should().Be("Subject");
        content.Body.Should().Be("Body");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidSubject_ThrowsArgumentException(string? subject)
    {
        Action act = () => EmailContent.Create(subject!, "Body");

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(subject));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidBody_ThrowsArgumentException(string? body)
    {
        Action act = () => EmailContent.Create("Subject", body!);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(body));
    }
}

public class EmailContentEqualityTests
{
    [Fact]
    public void Equals_SameSubjectAndBody_ReturnsTrue()
    {
        EmailContent content1 = EmailContent.Create("Subject", "Body");
        EmailContent content2 = EmailContent.Create("Subject", "Body");

        content1.Should().Be(content2);
    }

    [Fact]
    public void Equals_DifferentSubject_ReturnsFalse()
    {
        EmailContent content1 = EmailContent.Create("Subject1", "Body");
        EmailContent content2 = EmailContent.Create("Subject2", "Body");

        content1.Should().NotBe(content2);
    }

    [Fact]
    public void Equals_DifferentBody_ReturnsFalse()
    {
        EmailContent content1 = EmailContent.Create("Subject", "Body1");
        EmailContent content2 = EmailContent.Create("Subject", "Body2");

        content1.Should().NotBe(content2);
    }
}
