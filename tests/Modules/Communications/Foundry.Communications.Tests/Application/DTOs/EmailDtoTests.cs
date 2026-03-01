using Foundry.Communications.Application.Channels.Email.DTOs;
using Foundry.Communications.Domain.Channels.Email.Enums;

namespace Foundry.Communications.Tests.Application.DTOs;

public class EmailDtoTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        Guid id = Guid.NewGuid();
        DateTime sentAt = DateTime.UtcNow;
        DateTime createdAt = DateTime.UtcNow.AddMinutes(-5);
        DateTime updatedAt = DateTime.UtcNow;

        EmailDto dto = new(
            id, "to@test.com", "from@test.com", "Subject",
            "<p>Body</p>", EmailStatus.Sent, sentAt,
            null, 0, createdAt, updatedAt);

        dto.Id.Should().Be(id);
        dto.To.Should().Be("to@test.com");
        dto.From.Should().Be("from@test.com");
        dto.Subject.Should().Be("Subject");
        dto.Body.Should().Be("<p>Body</p>");
        dto.Status.Should().Be(EmailStatus.Sent);
        dto.SentAt.Should().Be(sentAt);
        dto.FailureReason.Should().BeNull();
        dto.RetryCount.Should().Be(0);
        dto.CreatedAt.Should().Be(createdAt);
        dto.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void Constructor_WithFailureReason_SetsFailureReason()
    {
        EmailDto dto = new(
            Guid.NewGuid(), "to@test.com", null, "Subject",
            "Body", EmailStatus.Failed, null,
            "SMTP connection timeout", 3,
            DateTime.UtcNow, DateTime.UtcNow);

        dto.From.Should().BeNull();
        dto.SentAt.Should().BeNull();
        dto.FailureReason.Should().Be("SMTP connection timeout");
        dto.RetryCount.Should().Be(3);
        dto.Status.Should().Be(EmailStatus.Failed);
    }

    [Fact]
    public void Constructor_WithNullOptionalFields_SetsToNull()
    {
        EmailDto dto = new(
            Guid.NewGuid(), "to@test.com", null, "Subject",
            "Body", EmailStatus.Pending, null,
            null, 0, DateTime.UtcNow, null);

        dto.From.Should().BeNull();
        dto.SentAt.Should().BeNull();
        dto.FailureReason.Should().BeNull();
        dto.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Equality_WithSameValues_AreEqual()
    {
        Guid id = Guid.NewGuid();
        DateTime createdAt = DateTime.UtcNow;

        EmailDto dto1 = new(
            id, "to@test.com", null, "Subject",
            "Body", EmailStatus.Pending, null,
            null, 0, createdAt, null);

        EmailDto dto2 = new(
            id, "to@test.com", null, "Subject",
            "Body", EmailStatus.Pending, null,
            null, 0, createdAt, null);

        dto1.Should().Be(dto2);
    }
}
