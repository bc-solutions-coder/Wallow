namespace Foundry.Shared.Kernel.Settings;

public static class SettingKeyValidator
{
    public const string CustomPrefix = "custom.";
    public const string SystemPrefix = "system.";
    public const int MaxCustomKeysPerTenant = 100;

    public static bool IsCustomKey(string key) => key.StartsWith(CustomPrefix, StringComparison.Ordinal);
    public static bool IsSystemKey(string key) => key.StartsWith(SystemPrefix, StringComparison.Ordinal);
    public static bool IsCodeDefinedKey(string key, ISettingRegistry registry) => registry.IsCodeDefinedKey(key);

    public static SettingKeyValidationResult Validate(string key, ISettingRegistry registry)
    {
        if (IsCustomKey(key))
        {
            return SettingKeyValidationResult.Custom;
        }

        if (IsSystemKey(key))
        {
            return SettingKeyValidationResult.System;
        }

        if (IsCodeDefinedKey(key, registry))
        {
            return SettingKeyValidationResult.CodeDefined;
        }

        return SettingKeyValidationResult.Unknown;
    }
}

public enum SettingKeyValidationResult { CodeDefined, Custom, System, Unknown }
