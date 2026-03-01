using Foundry.Identity.Application.DTOs;
using Foundry.Identity.Application.Interfaces;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Identity.Application.Commands.RotateServiceAccountSecret;

public sealed class RotateServiceAccountSecretHandler(IServiceAccountService serviceAccountService)
{
    public async Task<Result<SecretRotatedResult>> Handle(
        RotateServiceAccountSecretCommand command,
        CancellationToken ct)
    {
        SecretRotatedResult result = await serviceAccountService.RotateSecretAsync(command.Id, ct);
        return Result.Success(result);
    }
}
