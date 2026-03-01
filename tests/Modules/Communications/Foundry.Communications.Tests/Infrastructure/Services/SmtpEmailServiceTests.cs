using Foundry.Communications.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Foundry.Communications.Tests.Infrastructure.Services;

public class SmtpEmailServiceTests
{
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailServiceTests()
    {
        _logger = Substitute.For<ILogger<SmtpEmailService>>();
    }

    private SmtpEmailService CreateService(SmtpSettings? settings = null)
    {
        SmtpSettings smtpSettings = settings ?? new SmtpSettings
        {
            Host = "invalid.host.that.does.not.exist",
            Port = 9999,
            MaxRetries = 1,
            TimeoutSeconds = 1,
            DefaultFromAddress = "test@foundry.local",
            DefaultFromName = "Foundry Test"
        };

        IOptions<SmtpSettings> options = Options.Create(smtpSettings);
        return new SmtpEmailService(options, _logger);
    }

    [Fact]
    public async Task SendAsync_WhenSmtpConnectionFails_ThrowsException()
    {
        SmtpEmailService service = CreateService();

        Func<Task> act = () => service.SendAsync(
            "recipient@test.com", null, "Test Subject", "<p>Test Body</p>");

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SendAsync_WhenSmtpConnectionFails_RetriesConfiguredTimes()
    {
        SmtpSettings settings = new()
        {
            Host = "invalid.host.that.does.not.exist",
            Port = 9999,
            MaxRetries = 2,
            TimeoutSeconds = 1,
            DefaultFromAddress = "test@foundry.local",
            DefaultFromName = "Foundry Test"
        };
        SmtpEmailService service = CreateService(settings);

        Func<Task> act = () => service.SendAsync(
            "recipient@test.com", null, "Test Subject", "<p>Test Body</p>");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to send email after 2 attempts*");
    }

    [Fact]
    public async Task SendAsync_WithCustomFrom_DoesNotThrowArgumentException()
    {
        SmtpEmailService service = CreateService();

        try
        {
            await service.SendAsync(
                "recipient@test.com", "custom@sender.com", "Test Subject", "<p>Test Body</p>");
        }
        catch (Exception ex)
        {
            ex.Should().NotBeOfType<ArgumentException>();
        }
    }

    [Fact]
    public async Task SendAsync_WithNullFrom_UsesDefaultFrom()
    {
        SmtpEmailService service = CreateService();

        try
        {
            await service.SendAsync(
                "recipient@test.com", null, "Test Subject", "<p>Test Body</p>");
        }
        catch (Exception ex)
        {
            ex.Should().NotBeOfType<NullReferenceException>();
        }
    }

    [Fact]
    public async Task SendWithAttachmentAsync_WhenAttachmentExceedsMaxSize_ThrowsInvalidOperationException()
    {
        SmtpEmailService service = CreateService();
        byte[] largeAttachment = new byte[11 * 1024 * 1024]; // 11MB

        Func<Task> act = () => service.SendWithAttachmentAsync(
            "recipient@test.com", null, "Subject", "<p>Body</p>",
            largeAttachment, "large-file.bin");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exceeds maximum allowed size*");
    }

    [Fact]
    public async Task SendWithAttachmentAsync_WhenAttachmentAtMaxSize_DoesNotThrowSizeException()
    {
        SmtpEmailService service = CreateService();
        byte[] maxAttachment = new byte[10 * 1024 * 1024]; // 10MB exactly

        try
        {
            await service.SendWithAttachmentAsync(
                "recipient@test.com", null, "Subject", "<p>Body</p>",
                maxAttachment, "max-file.bin");
        }
        catch (InvalidOperationException ex)
        {
            ex.Message.Should().NotContain("exceeds maximum allowed size");
        }
    }

    [Fact]
    public async Task SendWithAttachmentAsync_WhenSmtpConnectionFails_ThrowsException()
    {
        SmtpEmailService service = CreateService();
        byte[] attachment = new byte[100];

        Func<Task> act = () => service.SendWithAttachmentAsync(
            "recipient@test.com", null, "Subject", "<p>Body</p>",
            attachment, "test.txt", "text/plain");

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SendWithAttachmentAsync_WithCustomContentType_FailsWithConnectionError()
    {
        SmtpEmailService service = CreateService();
        byte[] attachment = new byte[100];

        try
        {
            await service.SendWithAttachmentAsync(
                "recipient@test.com", "custom@sender.com", "Subject", "<p>Body</p>",
                attachment, "report.pdf", "application/pdf");
        }
        catch (Exception ex)
        {
            ex.Should().NotBeOfType<ArgumentException>();
        }
    }

    [Fact]
    public async Task SendAsync_WithEmptyFrom_UsesDefaultFrom()
    {
        SmtpEmailService service = CreateService();

        try
        {
            await service.SendAsync(
                "recipient@test.com", "", "Subject", "<p>Body</p>");
        }
        catch (Exception ex)
        {
            ex.Should().NotBeOfType<NullReferenceException>();
        }
    }

    [Fact]
    public async Task SendAsync_WithWhitespaceFrom_UsesDefaultFrom()
    {
        SmtpEmailService service = CreateService();

        try
        {
            await service.SendAsync(
                "recipient@test.com", "  ", "Subject", "<p>Body</p>");
        }
        catch (Exception ex)
        {
            ex.Should().NotBeOfType<NullReferenceException>();
        }
    }

    [Fact]
    public void Constructor_WithValidSettings_DoesNotThrow()
    {
        SmtpSettings settings = new()
        {
            Host = "smtp.example.com",
            Port = 587,
            UseSsl = true,
            Username = "user",
            Password = "pass"
        };

        IOptions<SmtpSettings> options = Options.Create(settings);

        SmtpEmailService service = new(options, _logger);

        service.Should().NotBeNull();
    }

    [Fact]
    public async Task SendAsync_WithMaxRetriesOne_FailsAfterSingleAttempt()
    {
        SmtpSettings settings = new()
        {
            Host = "invalid.host.that.does.not.exist",
            Port = 9999,
            MaxRetries = 1,
            TimeoutSeconds = 1,
            DefaultFromAddress = "test@foundry.local",
            DefaultFromName = "Foundry Test"
        };
        SmtpEmailService service = CreateService(settings);

        Func<Task> act = () => service.SendAsync(
            "to@test.com", null, "Subject", "Body");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to send email after 1 attempts*");
    }

    [Fact]
    public async Task SendWithAttachmentAsync_WithSmallAttachment_FailsWithConnectionError()
    {
        SmtpEmailService service = CreateService();
        byte[] attachment = new byte[10];

        Func<Task> act = () => service.SendWithAttachmentAsync(
            "recipient@test.com", null, "Subject", "<p>Body</p>",
            attachment, "small.txt");

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SendAsync_WithSslEnabled_FailsWithConnectionError()
    {
        SmtpSettings settings = new()
        {
            Host = "invalid.host.that.does.not.exist",
            Port = 465,
            UseSsl = true,
            MaxRetries = 1,
            TimeoutSeconds = 1,
            DefaultFromAddress = "test@foundry.local",
            DefaultFromName = "Foundry Test"
        };
        SmtpEmailService service = CreateService(settings);

        Func<Task> act = () => service.SendAsync(
            "to@test.com", null, "Subject", "Body");

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SendAsync_WithCredentials_FailsWithConnectionError()
    {
        SmtpSettings settings = new()
        {
            Host = "invalid.host.that.does.not.exist",
            Port = 587,
            UseSsl = false,
            Username = "user",
            Password = "pass",
            MaxRetries = 1,
            TimeoutSeconds = 1,
            DefaultFromAddress = "test@foundry.local",
            DefaultFromName = "Foundry Test"
        };
        SmtpEmailService service = CreateService(settings);

        Func<Task> act = () => service.SendAsync(
            "to@test.com", null, "Subject", "Body");

        await act.Should().ThrowAsync<Exception>();
    }
}
