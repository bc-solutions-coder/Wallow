using Foundry.Identity.Application.Interfaces;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Identity.Application.Commands.UpdateServiceAccountScopes;

public sealed class UpdateServiceAccountScopesHandler(IServiceAccountService serviceAccountService)
{
    public async Task<Result> Handle(
        UpdateServiceAccountScopesCommand command,
        CancellationToken ct)
    {
        await serviceAccountService.UpdateScopesAsync(command.Id, command.Scopes, ct);
        return Result.Success();
    }
}
