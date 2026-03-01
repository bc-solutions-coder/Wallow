using Foundry.Configuration.Api.Contracts.Enums;
using Foundry.Configuration.Domain.Enums;

namespace Foundry.Configuration.Api.Mappings;

/// <summary>Extension methods for mapping between API and Domain enums.</summary>
public static class EnumMappings
{
    public static FlagType ToDomain(this ApiFlagType api) => api switch
    {
        ApiFlagType.Boolean => FlagType.Boolean,
        ApiFlagType.Percentage => FlagType.Percentage,
        ApiFlagType.Variant => FlagType.Variant,
        _ => throw new ArgumentOutOfRangeException(nameof(api), api, "Unknown flag type")
    };

    public static ApiFlagType ToApi(this FlagType domain) => domain switch
    {
        FlagType.Boolean => ApiFlagType.Boolean,
        FlagType.Percentage => ApiFlagType.Percentage,
        FlagType.Variant => ApiFlagType.Variant,
        _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, "Unknown flag type")
    };
}
