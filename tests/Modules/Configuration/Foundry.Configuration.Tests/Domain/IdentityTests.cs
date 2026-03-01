using Foundry.Configuration.Domain.Identity;

namespace Foundry.Configuration.Tests.Domain;

public class FeatureFlagIdTests
{
    [Fact]
    public void New_GeneratesUniqueIds()
    {
        FeatureFlagId id1 = FeatureFlagId.New();
        FeatureFlagId id2 = FeatureFlagId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBe(id2.Value);
    }

    [Fact]
    public void Create_WithGuid_SetsValue()
    {
        Guid guid = Guid.NewGuid();

        FeatureFlagId id = FeatureFlagId.Create(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        Guid guid = Guid.NewGuid();
        FeatureFlagId id1 = FeatureFlagId.Create(guid);
        FeatureFlagId id2 = FeatureFlagId.Create(guid);

        id1.Should().Be(id2);
    }
}

public class FeatureFlagOverrideIdTests
{
    [Fact]
    public void New_GeneratesUniqueIds()
    {
        FeatureFlagOverrideId id1 = FeatureFlagOverrideId.New();
        FeatureFlagOverrideId id2 = FeatureFlagOverrideId.New();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void Create_WithGuid_SetsValue()
    {
        Guid guid = Guid.NewGuid();

        FeatureFlagOverrideId id = FeatureFlagOverrideId.Create(guid);

        id.Value.Should().Be(guid);
    }
}

public class CustomFieldDefinitionIdTests
{
    [Fact]
    public void New_GeneratesUniqueIds()
    {
        CustomFieldDefinitionId id1 = CustomFieldDefinitionId.New();
        CustomFieldDefinitionId id2 = CustomFieldDefinitionId.New();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void Create_WithGuid_SetsValue()
    {
        Guid guid = Guid.NewGuid();

        CustomFieldDefinitionId id = CustomFieldDefinitionId.Create(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        Guid guid = Guid.NewGuid();
        CustomFieldDefinitionId id1 = CustomFieldDefinitionId.Create(guid);
        CustomFieldDefinitionId id2 = CustomFieldDefinitionId.Create(guid);

        id1.Should().Be(id2);
    }
}
