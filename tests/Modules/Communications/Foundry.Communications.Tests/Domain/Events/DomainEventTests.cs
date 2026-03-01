using Foundry.Communications.Domain.Channels.Email.Events;
using Foundry.Communications.Domain.Channels.InApp.Events;

namespace Foundry.Communications.Tests.Domain.Events;

public class EmailSentDomainEventTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        Guid emailMessageId = Guid.NewGuid();

        EmailSentDomainEvent domainEvent = new(emailMessageId, "test@example.com", "Test Subject");

        domainEvent.EmailMessageId.Should().Be(emailMessageId);
        domainEvent.ToAddress.Should().Be("test@example.com");
        domainEvent.Subject.Should().Be("Test Subject");
    }
}

public class EmailFailedDomainEventTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        Guid emailMessageId = Guid.NewGuid();

        EmailFailedDomainEvent domainEvent = new(emailMessageId, "test@example.com", "SMTP error", 2);

        domainEvent.EmailMessageId.Should().Be(emailMessageId);
        domainEvent.ToAddress.Should().Be("test@example.com");
        domainEvent.FailureReason.Should().Be("SMTP error");
        domainEvent.RetryCount.Should().Be(2);
    }
}

public class NotificationCreatedDomainEventTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        Guid notificationId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        NotificationCreatedDomainEvent domainEvent = new(notificationId, userId, "Alert", "SystemAlert");

        domainEvent.NotificationId.Should().Be(notificationId);
        domainEvent.UserId.Should().Be(userId);
        domainEvent.Title.Should().Be("Alert");
        domainEvent.Type.Should().Be("SystemAlert");
    }
}

public class NotificationReadDomainEventTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        Guid notificationId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        NotificationReadDomainEvent domainEvent = new(notificationId, userId);

        domainEvent.NotificationId.Should().Be(notificationId);
        domainEvent.UserId.Should().Be(userId);
    }
}
