using Foundry.Communications.Domain.Announcements.Entities;
using Foundry.Communications.Domain.Announcements.Enums;
using Foundry.Communications.Domain.Announcements.Identity;

namespace Foundry.Communications.Tests.Domain.Announcements;

public class ChangelogItemCreateTests
{
    [Fact]
    public void Create_WithValidData_ReturnsItem()
    {
        ChangelogEntryId entryId = ChangelogEntryId.New();

        ChangelogItem item = ChangelogItem.Create(entryId, "Added new feature", ChangeType.Feature);

        item.EntryId.Should().Be(entryId);
        item.Description.Should().Be("Added new feature");
        item.Type.Should().Be(ChangeType.Feature);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidDescription_ThrowsArgumentException(string? description)
    {
        Action act = () => ChangelogItem.Create(ChangelogEntryId.New(), description!, ChangeType.Feature);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_GeneratesUniqueId()
    {
        ChangelogEntryId entryId = ChangelogEntryId.New();

        ChangelogItem first = ChangelogItem.Create(entryId, "Item 1", ChangeType.Feature);
        ChangelogItem second = ChangelogItem.Create(entryId, "Item 2", ChangeType.Fix);

        first.Id.Should().NotBe(second.Id);
    }
}

public class ChangelogItemUpdateTests
{
    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        ChangelogItem item = ChangelogItem.Create(ChangelogEntryId.New(), "Original", ChangeType.Feature);

        item.Update("Updated description", ChangeType.Fix);

        item.Description.Should().Be("Updated description");
        item.Type.Should().Be(ChangeType.Fix);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithInvalidDescription_ThrowsArgumentException(string? description)
    {
        ChangelogItem item = ChangelogItem.Create(ChangelogEntryId.New(), "Original", ChangeType.Feature);

        Action act = () => item.Update(description!, ChangeType.Fix);

        act.Should().Throw<ArgumentException>();
    }
}
