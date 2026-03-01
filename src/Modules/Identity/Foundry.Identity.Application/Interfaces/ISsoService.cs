using Foundry.Identity.Application.DTOs;

namespace Foundry.Identity.Application.Interfaces;

/// <summary>
/// Service for managing SSO (Single Sign-On) configuration and integration.
/// </summary>
public interface ISsoService
{
    /// <summary>
    /// Gets the SSO configuration for the current tenant.
    /// </summary>
    Task<SsoConfigurationDto?> GetConfigurationAsync(CancellationToken ct = default);

    /// <summary>
    /// Saves SAML SSO configuration for the current tenant.
    /// </summary>
    Task<SsoConfigurationDto> SaveSamlConfigurationAsync(SaveSamlConfigRequest request, CancellationToken ct = default);

    /// <summary>
    /// Saves OIDC SSO configuration for the current tenant.
    /// </summary>
    Task<SsoConfigurationDto> SaveOidcConfigurationAsync(SaveOidcConfigRequest request, CancellationToken ct = default);

    /// <summary>
    /// Tests the SSO connection to verify configuration is correct.
    /// </summary>
    Task<SsoTestResult> TestConnectionAsync(CancellationToken ct = default);

    /// <summary>
    /// Activates SSO for the current tenant.
    /// </summary>
    Task ActivateAsync(CancellationToken ct = default);

    /// <summary>
    /// Disables SSO for the current tenant.
    /// </summary>
    Task DisableAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the SAML Service Provider metadata XML for the current tenant.
    /// Used by enterprise IdPs to configure their side of the SSO integration.
    /// </summary>
    Task<string> GetSamlServiceProviderMetadataAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets OIDC callback information for the current tenant.
    /// Used by enterprise IdPs to configure redirect URIs.
    /// </summary>
    Task<OidcCallbackInfo> GetOidcCallbackInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// Validates the IdP metadata or discovery endpoint.
    /// </summary>
    Task<SsoValidationResult> ValidateIdpConfigurationAsync(CancellationToken ct = default);

    /// <summary>
    /// Synchronizes user claims from the IdP to local configuration.
    /// </summary>
    Task SyncUserClaimsAsync(Guid userId, CancellationToken ct = default);
}
