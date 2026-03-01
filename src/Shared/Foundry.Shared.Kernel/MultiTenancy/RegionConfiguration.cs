namespace Foundry.Shared.Kernel.MultiTenancy;

public static class RegionConfiguration
{
    public const string US_EAST = "us-east-1";
    public const string EU_WEST = "eu-west-1";
    public const string AP_SOUTHEAST = "ap-southeast-1";

    public const string PrimaryRegion = US_EAST;
}

public record RegionSettings(string Name, bool IsPrimary, bool IsActive);
