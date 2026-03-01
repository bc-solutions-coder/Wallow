namespace Foundry.Identity.Api.Contracts.Requests;

public record UpdateScopesRequest(IReadOnlyList<string> Scopes);
