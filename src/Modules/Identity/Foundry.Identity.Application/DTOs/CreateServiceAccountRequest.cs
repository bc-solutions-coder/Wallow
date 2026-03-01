namespace Foundry.Identity.Application.DTOs;

public record CreateServiceAccountRequest(
    string Name,
    string? Description,
    IEnumerable<string> Scopes);
