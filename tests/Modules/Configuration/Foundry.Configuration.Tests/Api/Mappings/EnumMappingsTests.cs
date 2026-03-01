using Foundry.Configuration.Api.Contracts.Enums;
using Foundry.Configuration.Api.Mappings;
using Foundry.Configuration.Domain.Enums;

namespace Foundry.Configuration.Tests.Api.Mappings;

public class EnumMappingsTests
{
    #region ToDomain

    [Theory]
    [InlineData(ApiFlagType.Boolean, FlagType.Boolean)]
    [InlineData(ApiFlagType.Percentage, FlagType.Percentage)]
    [InlineData(ApiFlagType.Variant, FlagType.Variant)]
    public void ToDomain_MapsCorrectly(ApiFlagType api, FlagType expected)
    {
        FlagType result = api.ToDomain();

        result.Should().Be(expected);
    }

    [Fact]
    public void ToDomain_WithInvalidValue_ThrowsArgumentOutOfRangeException()
    {
        ApiFlagType invalid = (ApiFlagType)999;

        Action act = () => invalid.ToDomain();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("api");
    }

    #endregion

    #region ToApi

    [Theory]
    [InlineData(FlagType.Boolean, ApiFlagType.Boolean)]
    [InlineData(FlagType.Percentage, ApiFlagType.Percentage)]
    [InlineData(FlagType.Variant, ApiFlagType.Variant)]
    public void ToApi_MapsCorrectly(FlagType domain, ApiFlagType expected)
    {
        ApiFlagType result = domain.ToApi();

        result.Should().Be(expected);
    }

    [Fact]
    public void ToApi_WithInvalidValue_ThrowsArgumentOutOfRangeException()
    {
        FlagType invalid = (FlagType)999;

        Action act = () => invalid.ToApi();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("domain");
    }

    #endregion

    #region Roundtrip

    [Theory]
    [InlineData(ApiFlagType.Boolean)]
    [InlineData(ApiFlagType.Percentage)]
    [InlineData(ApiFlagType.Variant)]
    public void Roundtrip_ApiToDomainAndBack_PreservesValue(ApiFlagType original)
    {
        ApiFlagType result = original.ToDomain().ToApi();

        result.Should().Be(original);
    }

    [Theory]
    [InlineData(FlagType.Boolean)]
    [InlineData(FlagType.Percentage)]
    [InlineData(FlagType.Variant)]
    public void Roundtrip_DomainToApiAndBack_PreservesValue(FlagType original)
    {
        FlagType result = original.ToApi().ToDomain();

        result.Should().Be(original);
    }

    #endregion
}
