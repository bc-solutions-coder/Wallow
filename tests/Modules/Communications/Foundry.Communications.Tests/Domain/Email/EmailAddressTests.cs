using Foundry.Communications.Domain.Channels.Email.Exceptions;
using Foundry.Communications.Domain.Channels.Email.ValueObjects;

namespace Foundry.Communications.Tests.Domain.Email;

public class EmailAddressCreateTests
{
    [Fact]
    public void Create_WithValidEmail_ReturnsEmailAddress()
    {
        EmailAddress email = EmailAddress.Create("user@example.com");

        email.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void Create_WithMixedCase_NormalizesToLowerCase()
    {
        EmailAddress email = EmailAddress.Create("User@EXAMPLE.Com");

        email.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void Create_WithLeadingTrailingSpaces_TrimsInput()
    {
        EmailAddress email = EmailAddress.Create("  user@example.com  ");

        email.Value.Should().Be("user@example.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ThrowsInvalidEmailAddressException(string? email)
    {
        Action act = () => EmailAddress.Create(email!);

        act.Should().Throw<InvalidEmailAddressException>();
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@domain")]
    [InlineData("@nodomain.com")]
    [InlineData("spaces in@email.com")]
    public void Create_WithInvalidFormat_ThrowsInvalidEmailAddressException(string email)
    {
        Action act = () => EmailAddress.Create(email);

        act.Should().Throw<InvalidEmailAddressException>();
    }
}

public class EmailAddressEqualityTests
{
    [Fact]
    public void Equals_SameEmail_ReturnsTrue()
    {
        EmailAddress email1 = EmailAddress.Create("user@example.com");
        EmailAddress email2 = EmailAddress.Create("user@example.com");

        email1.Should().Be(email2);
    }

    [Fact]
    public void Equals_SameEmailDifferentCase_ReturnsTrue()
    {
        EmailAddress email1 = EmailAddress.Create("user@example.com");
        EmailAddress email2 = EmailAddress.Create("USER@EXAMPLE.COM");

        email1.Should().Be(email2);
    }

    [Fact]
    public void Equals_DifferentEmail_ReturnsFalse()
    {
        EmailAddress email1 = EmailAddress.Create("user1@example.com");
        EmailAddress email2 = EmailAddress.Create("user2@example.com");

        email1.Should().NotBe(email2);
    }
}

public class EmailAddressConversionTests
{
    [Fact]
    public void ToString_ReturnsEmailValue()
    {
        EmailAddress email = EmailAddress.Create("user@example.com");

        email.ToString().Should().Be("user@example.com");
    }

    [Fact]
    public void ImplicitConversion_ReturnsStringValue()
    {
        EmailAddress email = EmailAddress.Create("user@example.com");

        string result = email;

        result.Should().Be("user@example.com");
    }
}
