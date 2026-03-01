using Foundry.Identity.Application.DTOs;
using Foundry.Identity.Application.Interfaces;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Identity.Application.Queries.GetServiceAccounts;

public sealed class GetServiceAccountsHandler(IServiceAccountService serviceAccountService)
{
    public async Task<Result<IReadOnlyList<ServiceAccountDto>>> Handle(
        GetServiceAccountsQuery _,
        CancellationToken ct)
    {
        IReadOnlyList<ServiceAccountDto> result = await serviceAccountService.ListAsync(ct);
        return Result.Success(result);
    }
}
