using Foundry.Identity.Domain.Identity;

namespace Foundry.Identity.Application.Commands.UpdateServiceAccountScopes;

public sealed record UpdateServiceAccountScopesCommand(
    ServiceAccountMetadataId Id,
    IEnumerable<string> Scopes);
