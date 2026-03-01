using Foundry.Identity.Domain.Identity;

namespace Foundry.Identity.Application.DTOs;

public record ServiceAccountCreatedResult(
    ServiceAccountMetadataId Id,
    string ClientId,
    string ClientSecret,
    string TokenEndpoint,
    IReadOnlyList<string> Scopes);
