using Foundry.Communications.Infrastructure.Services;

namespace Foundry.Communications.Tests.Infrastructure.Services;

public class SmtpSettingsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        SmtpSettings settings = new();

        settings.Host.Should().Be("localhost");
        settings.Port.Should().Be(1025);
        settings.UseSsl.Should().BeFalse();
        settings.Username.Should().BeNull();
        settings.Password.Should().BeNull();
        settings.DefaultFromAddress.Should().Be("noreply@foundry.local");
        settings.DefaultFromName.Should().Be("Foundry");
        settings.MaxRetries.Should().Be(3);
        settings.TimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        SmtpSettings settings = new()
        {
            Host = "smtp.example.com",
            Port = 587,
            UseSsl = true,
            Username = "user",
            Password = "pass",
            DefaultFromAddress = "admin@example.com",
            DefaultFromName = "Admin",
            MaxRetries = 5,
            TimeoutSeconds = 60
        };

        settings.Host.Should().Be("smtp.example.com");
        settings.Port.Should().Be(587);
        settings.UseSsl.Should().BeTrue();
        settings.Username.Should().Be("user");
        settings.Password.Should().Be("pass");
        settings.DefaultFromAddress.Should().Be("admin@example.com");
        settings.DefaultFromName.Should().Be("Admin");
        settings.MaxRetries.Should().Be(5);
        settings.TimeoutSeconds.Should().Be(60);
    }
}
