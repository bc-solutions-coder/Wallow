namespace Foundry.Configuration.Api.Contracts.Enums;

/// <summary>Type of feature flag for API contracts.</summary>
public enum ApiFlagType
{
    /// <summary>Simple on/off toggle.</summary>
    Boolean = 0,

    /// <summary>Percentage-based rollout (0-100).</summary>
    Percentage = 1,

    /// <summary>Multiple variants for A/B testing.</summary>
    Variant = 2
}
