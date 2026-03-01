using Foundry.Communications.Application.Announcements.DTOs;
using Foundry.Communications.Domain.Announcements.Enums;

namespace Foundry.Communications.Tests.Application.DTOs;

public class AnnouncementDtoTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        Guid id = Guid.NewGuid();
        DateTime publishAt = DateTime.UtcNow.AddHours(1);
        DateTime expiresAt = DateTime.UtcNow.AddDays(7);
        DateTime createdAt = DateTime.UtcNow;

        AnnouncementDto dto = new(
            id, "Title", "Content", AnnouncementType.Alert,
            AnnouncementTarget.Tenant, "tenant-123",
            publishAt, expiresAt, true, false,
            "https://example.com/action", "Click Here",
            "https://example.com/image.png",
            AnnouncementStatus.Published, createdAt);

        dto.Id.Should().Be(id);
        dto.Title.Should().Be("Title");
        dto.Content.Should().Be("Content");
        dto.Type.Should().Be(AnnouncementType.Alert);
        dto.Target.Should().Be(AnnouncementTarget.Tenant);
        dto.TargetValue.Should().Be("tenant-123");
        dto.PublishAt.Should().Be(publishAt);
        dto.ExpiresAt.Should().Be(expiresAt);
        dto.IsPinned.Should().BeTrue();
        dto.IsDismissible.Should().BeFalse();
        dto.ActionUrl.Should().Be("https://example.com/action");
        dto.ActionLabel.Should().Be("Click Here");
        dto.ImageUrl.Should().Be("https://example.com/image.png");
        dto.Status.Should().Be(AnnouncementStatus.Published);
        dto.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Constructor_WithNullOptionalFields_SetsToNull()
    {
        AnnouncementDto dto = new(
            Guid.NewGuid(), "Title", "Content", AnnouncementType.Feature,
            AnnouncementTarget.All, null, null, null,
            false, true, null, null, null,
            AnnouncementStatus.Draft, DateTime.UtcNow);

        dto.TargetValue.Should().BeNull();
        dto.PublishAt.Should().BeNull();
        dto.ExpiresAt.Should().BeNull();
        dto.ActionUrl.Should().BeNull();
        dto.ActionLabel.Should().BeNull();
        dto.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void Equality_WithSameValues_AreEqual()
    {
        Guid id = Guid.NewGuid();
        DateTime createdAt = DateTime.UtcNow;

        AnnouncementDto dto1 = new(
            id, "Title", "Content", AnnouncementType.Feature,
            AnnouncementTarget.All, null, null, null,
            false, true, null, null, null,
            AnnouncementStatus.Draft, createdAt);

        AnnouncementDto dto2 = new(
            id, "Title", "Content", AnnouncementType.Feature,
            AnnouncementTarget.All, null, null, null,
            false, true, null, null, null,
            AnnouncementStatus.Draft, createdAt);

        dto1.Should().Be(dto2);
    }
}
