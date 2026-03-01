using Foundry.Communications.Domain.Announcements.Entities;
using Foundry.Communications.Domain.Announcements.Enums;

namespace Foundry.Communications.Tests.Domain.Announcements;

public class AnnouncementStatusTests
{
    [Fact]
    public void Publish_FromDraft_ChangesStatusToPublishedAndSetsPublishAt()
    {
        Announcement announcement = Announcement.Create(
            "Title", "Content", AnnouncementType.Feature);
        announcement.Status.Should().Be(AnnouncementStatus.Draft);
        DateTime before = DateTime.UtcNow;

        announcement.Publish();

        announcement.Status.Should().Be(AnnouncementStatus.Published);
        announcement.PublishAt.Should().NotBeNull();
        announcement.PublishAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_DoesNothing()
    {
        Announcement announcement = Announcement.Create(
            "Title", "Content", AnnouncementType.Feature);
        announcement.Publish();
        DateTime? firstPublishAt = announcement.PublishAt;

        announcement.Publish();

        announcement.Status.Should().Be(AnnouncementStatus.Published);
        announcement.PublishAt.Should().Be(firstPublishAt);
    }

    [Fact]
    public void Expire_ChangesStatusToExpired()
    {
        Announcement announcement = Announcement.Create(
            "Title", "Content", AnnouncementType.Feature);
        announcement.Publish();

        announcement.Expire();

        announcement.Status.Should().Be(AnnouncementStatus.Expired);
    }

    [Fact]
    public void Archive_ChangesStatusToArchived()
    {
        Announcement announcement = Announcement.Create(
            "Title", "Content", AnnouncementType.Feature);

        announcement.Archive();

        announcement.Status.Should().Be(AnnouncementStatus.Archived);
    }

    [Fact]
    public void Expire_SetsUpdatedAtTimestamp()
    {
        Announcement announcement = Announcement.Create(
            "Title", "Content", AnnouncementType.Feature);
        DateTime before = DateTime.UtcNow;

        announcement.Expire();

        announcement.UpdatedAt.Should().NotBeNull();
        announcement.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Archive_SetsUpdatedAtTimestamp()
    {
        Announcement announcement = Announcement.Create(
            "Title", "Content", AnnouncementType.Feature);
        DateTime before = DateTime.UtcNow;

        announcement.Archive();

        announcement.UpdatedAt.Should().NotBeNull();
        announcement.UpdatedAt.Should().BeOnOrAfter(before);
    }
}
