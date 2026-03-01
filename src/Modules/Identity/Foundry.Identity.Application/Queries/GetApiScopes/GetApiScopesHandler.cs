using Foundry.Identity.Application.DTOs;
using Foundry.Identity.Application.Interfaces;
using Foundry.Identity.Domain.Entities;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Identity.Application.Queries.GetApiScopes;

public sealed class GetApiScopesHandler(IApiScopeRepository apiScopeRepository)
{
    public async Task<Result<IReadOnlyList<ApiScopeDto>>> Handle(
        GetApiScopesQuery query,
        CancellationToken ct)
    {
        IReadOnlyList<ApiScope> scopes = await apiScopeRepository.GetAllAsync(query.Category, ct);

        List<ApiScopeDto> dtos = scopes
            .Select(s => new ApiScopeDto(
                s.Id,
                s.Code,
                s.DisplayName,
                s.Category,
                s.Description,
                s.IsDefault))
            .ToList();

        return Result.Success<IReadOnlyList<ApiScopeDto>>(dtos);
    }
}
