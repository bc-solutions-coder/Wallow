using Foundry.Identity.Application.DTOs;
using Foundry.Identity.Application.Interfaces;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Identity.Application.Queries.GetServiceAccount;

public sealed class GetServiceAccountHandler(IServiceAccountService serviceAccountService)
{
    public async Task<Result<ServiceAccountDto?>> Handle(
        GetServiceAccountQuery query,
        CancellationToken ct)
    {
        ServiceAccountDto? result = await serviceAccountService.GetAsync(query.Id, ct);
        return Result.Success(result);
    }
}
