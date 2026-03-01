using Foundry.Identity.Application.Interfaces;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Identity.Application.Commands.RevokeServiceAccount;

public sealed class RevokeServiceAccountHandler(IServiceAccountService serviceAccountService)
{
    public async Task<Result> Handle(
        RevokeServiceAccountCommand command,
        CancellationToken ct)
    {
        await serviceAccountService.RevokeAsync(command.Id, ct);
        return Result.Success();
    }
}
