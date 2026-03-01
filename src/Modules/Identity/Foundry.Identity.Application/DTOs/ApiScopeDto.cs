using Foundry.Identity.Domain.Identity;

namespace Foundry.Identity.Application.DTOs;

public record ApiScopeDto(
    ApiScopeId Id,
    string Code,
    string DisplayName,
    string Category,
    string? Description,
    bool IsDefault);
