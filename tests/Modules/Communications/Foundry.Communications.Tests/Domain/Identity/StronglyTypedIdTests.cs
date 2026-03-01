using Foundry.Communications.Domain.Announcements.Identity;
using Foundry.Communications.Domain.Channels.Email.Identity;
using Foundry.Communications.Domain.Channels.InApp.Identity;

namespace Foundry.Communications.Tests.Domain.Identity;

public class CommunicationsStronglyTypedIdTests
{
    [Fact]
    public void AnnouncementId_New_GeneratesUniqueIds()
    {
        AnnouncementId id1 = AnnouncementId.New();
        AnnouncementId id2 = AnnouncementId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void AnnouncementId_Create_WrapsGuid()
    {
        Guid guid = Guid.NewGuid();

        AnnouncementId id = AnnouncementId.Create(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void ChangelogEntryId_New_GeneratesUniqueIds()
    {
        ChangelogEntryId id1 = ChangelogEntryId.New();
        ChangelogEntryId id2 = ChangelogEntryId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void ChangelogEntryId_Create_WrapsGuid()
    {
        Guid guid = Guid.NewGuid();

        ChangelogEntryId id = ChangelogEntryId.Create(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void ChangelogItemId_New_GeneratesUniqueIds()
    {
        ChangelogItemId id1 = ChangelogItemId.New();
        ChangelogItemId id2 = ChangelogItemId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void ChangelogItemId_Create_WrapsGuid()
    {
        Guid guid = Guid.NewGuid();

        ChangelogItemId id = ChangelogItemId.Create(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void AnnouncementDismissalId_New_GeneratesUniqueIds()
    {
        AnnouncementDismissalId id1 = AnnouncementDismissalId.New();
        AnnouncementDismissalId id2 = AnnouncementDismissalId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void AnnouncementDismissalId_Create_WrapsGuid()
    {
        Guid guid = Guid.NewGuid();

        AnnouncementDismissalId id = AnnouncementDismissalId.Create(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void EmailMessageId_New_GeneratesUniqueIds()
    {
        EmailMessageId id1 = EmailMessageId.New();
        EmailMessageId id2 = EmailMessageId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void EmailMessageId_Create_WrapsGuid()
    {
        Guid guid = Guid.NewGuid();

        EmailMessageId id = EmailMessageId.Create(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void EmailPreferenceId_New_GeneratesUniqueIds()
    {
        EmailPreferenceId id1 = EmailPreferenceId.New();
        EmailPreferenceId id2 = EmailPreferenceId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void EmailPreferenceId_Create_WrapsGuid()
    {
        Guid guid = Guid.NewGuid();

        EmailPreferenceId id = EmailPreferenceId.Create(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void NotificationId_New_GeneratesUniqueIds()
    {
        NotificationId id1 = NotificationId.New();
        NotificationId id2 = NotificationId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void NotificationId_Create_WrapsGuid()
    {
        Guid guid = Guid.NewGuid();

        NotificationId id = NotificationId.Create(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void SameId_Equals_ReturnsTrue()
    {
        Guid guid = Guid.NewGuid();
        AnnouncementId id1 = AnnouncementId.Create(guid);
        AnnouncementId id2 = AnnouncementId.Create(guid);

        id1.Should().Be(id2);
    }

    [Fact]
    public void DifferentId_Equals_ReturnsFalse()
    {
        AnnouncementId id1 = AnnouncementId.New();
        AnnouncementId id2 = AnnouncementId.New();

        id1.Should().NotBe(id2);
    }
}
