using Foundry.Communications.Domain.Announcements.Entities;
using Foundry.Communications.Domain.Announcements.Enums;
using Foundry.Communications.Domain.Announcements.Identity;

namespace Foundry.Communications.Tests.Domain.Announcements;

public class ChangelogEntryCreateTests
{
    [Fact]
    public void Create_WithValidData_ReturnsUnpublishedEntry()
    {
        DateTime releasedAt = DateTime.UtcNow;

        ChangelogEntry entry = ChangelogEntry.Create("1.0.0", "Initial Release", "First version", releasedAt);

        entry.Version.Should().Be("1.0.0");
        entry.Title.Should().Be("Initial Release");
        entry.Content.Should().Be("First version");
        entry.ReleasedAt.Should().Be(releasedAt);
        entry.IsPublished.Should().BeFalse();
        entry.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidVersion_ThrowsArgumentException(string? version)
    {
        Action act = () => ChangelogEntry.Create(version!, "Title", "Content", DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidTitle_ThrowsArgumentException(string? title)
    {
        Action act = () => ChangelogEntry.Create("1.0.0", title!, "Content", DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidContent_ThrowsArgumentException(string? content)
    {
        Action act = () => ChangelogEntry.Create("1.0.0", "Title", content!, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_SetsCreatedAtTimestamp()
    {
        DateTime before = DateTime.UtcNow;

        ChangelogEntry entry = ChangelogEntry.Create("1.0.0", "Title", "Content", DateTime.UtcNow);

        entry.CreatedAt.Should().BeOnOrAfter(before);
    }
}

public class ChangelogEntryUpdateTests
{
    [Fact]
    public void Update_WithValidData_UpdatesAllProperties()
    {
        ChangelogEntry entry = ChangelogEntry.Create("1.0.0", "Title", "Content", DateTime.UtcNow);
        DateTime newReleasedAt = DateTime.UtcNow.AddDays(1);

        entry.Update("2.0.0", "Updated Title", "Updated Content", newReleasedAt);

        entry.Version.Should().Be("2.0.0");
        entry.Title.Should().Be("Updated Title");
        entry.Content.Should().Be("Updated Content");
        entry.ReleasedAt.Should().Be(newReleasedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithInvalidVersion_ThrowsArgumentException(string? version)
    {
        ChangelogEntry entry = ChangelogEntry.Create("1.0.0", "Title", "Content", DateTime.UtcNow);

        Action act = () => entry.Update(version!, "Title", "Content", DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithInvalidTitle_ThrowsArgumentException(string? title)
    {
        ChangelogEntry entry = ChangelogEntry.Create("1.0.0", "Title", "Content", DateTime.UtcNow);

        Action act = () => entry.Update("1.0.0", title!, "Content", DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithInvalidContent_ThrowsArgumentException(string? content)
    {
        ChangelogEntry entry = ChangelogEntry.Create("1.0.0", "Title", "Content", DateTime.UtcNow);

        Action act = () => entry.Update("1.0.0", "Title", content!, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_SetsUpdatedAtTimestamp()
    {
        ChangelogEntry entry = ChangelogEntry.Create("1.0.0", "Title", "Content", DateTime.UtcNow);
        DateTime before = DateTime.UtcNow;

        entry.Update("2.0.0", "New Title", "New Content", DateTime.UtcNow);

        entry.UpdatedAt.Should().NotBeNull();
        entry.UpdatedAt.Should().BeOnOrAfter(before);
    }
}

public class ChangelogEntryPublishTests
{
    [Fact]
    public void Publish_SetsIsPublishedToTrue()
    {
        ChangelogEntry entry = ChangelogEntry.Create("1.0.0", "Title", "Content", DateTime.UtcNow);

        entry.Publish();

        entry.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Unpublish_SetsIsPublishedToFalse()
    {
        ChangelogEntry entry = ChangelogEntry.Create("1.0.0", "Title", "Content", DateTime.UtcNow);
        entry.Publish();

        entry.Unpublish();

        entry.IsPublished.Should().BeFalse();
    }

    [Fact]
    public void Publish_SetsUpdatedAtTimestamp()
    {
        ChangelogEntry entry = ChangelogEntry.Create("1.0.0", "Title", "Content", DateTime.UtcNow);
        DateTime before = DateTime.UtcNow;

        entry.Publish();

        entry.UpdatedAt.Should().NotBeNull();
        entry.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Unpublish_SetsUpdatedAtTimestamp()
    {
        ChangelogEntry entry = ChangelogEntry.Create("1.0.0", "Title", "Content", DateTime.UtcNow);
        entry.Publish();
        DateTime before = DateTime.UtcNow;

        entry.Unpublish();

        entry.UpdatedAt.Should().NotBeNull();
        entry.UpdatedAt.Should().BeOnOrAfter(before);
    }
}

public class ChangelogEntryItemTests
{
    [Fact]
    public void AddItem_AddsItemToCollection()
    {
        ChangelogEntry entry = ChangelogEntry.Create("1.0.0", "Title", "Content", DateTime.UtcNow);

        ChangelogItem item = entry.AddItem("Added dark mode", ChangeType.Feature);

        entry.Items.Should().ContainSingle();
        entry.Items.Should().Contain(item);
        item.Description.Should().Be("Added dark mode");
        item.Type.Should().Be(ChangeType.Feature);
        item.EntryId.Should().Be(entry.Id);
    }

    [Fact]
    public void AddItem_MultipleItems_AddsAll()
    {
        ChangelogEntry entry = ChangelogEntry.Create("1.0.0", "Title", "Content", DateTime.UtcNow);

        entry.AddItem("Feature A", ChangeType.Feature);
        entry.AddItem("Fix B", ChangeType.Fix);
        entry.AddItem("Security C", ChangeType.Security);

        entry.Items.Should().HaveCount(3);
    }

    [Fact]
    public void AddItem_SetsUpdatedAtTimestamp()
    {
        ChangelogEntry entry = ChangelogEntry.Create("1.0.0", "Title", "Content", DateTime.UtcNow);
        DateTime before = DateTime.UtcNow;

        entry.AddItem("New feature", ChangeType.Feature);

        entry.UpdatedAt.Should().NotBeNull();
        entry.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void RemoveItem_ExistingItem_RemovesFromCollection()
    {
        ChangelogEntry entry = ChangelogEntry.Create("1.0.0", "Title", "Content", DateTime.UtcNow);
        ChangelogItem item = entry.AddItem("Feature", ChangeType.Feature);

        entry.RemoveItem(item.Id);

        entry.Items.Should().BeEmpty();
    }

    [Fact]
    public void RemoveItem_NonExistentItem_DoesNothing()
    {
        ChangelogEntry entry = ChangelogEntry.Create("1.0.0", "Title", "Content", DateTime.UtcNow);
        entry.AddItem("Feature", ChangeType.Feature);

        entry.RemoveItem(ChangelogItemId.New());

        entry.Items.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveItem_ExistingItem_SetsUpdatedAtTimestamp()
    {
        ChangelogEntry entry = ChangelogEntry.Create("1.0.0", "Title", "Content", DateTime.UtcNow);
        ChangelogItem item = entry.AddItem("Feature", ChangeType.Feature);
        DateTime before = DateTime.UtcNow;

        entry.RemoveItem(item.Id);

        entry.UpdatedAt.Should().NotBeNull();
        entry.UpdatedAt.Should().BeOnOrAfter(before);
    }
}
