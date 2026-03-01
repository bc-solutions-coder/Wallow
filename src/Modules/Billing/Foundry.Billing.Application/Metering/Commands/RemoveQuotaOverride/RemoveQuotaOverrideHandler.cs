using Foundry.Billing.Application.Metering.Interfaces;
using Foundry.Billing.Domain.Metering.Entities;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Billing.Application.Metering.Commands.RemoveQuotaOverride;

public sealed class RemoveQuotaOverrideHandler(IQuotaDefinitionRepository quotaRepository)
{
    public async Task<Result> Handle(RemoveQuotaOverrideCommand command, CancellationToken cancellationToken)
    {
        QuotaDefinition? existing = await quotaRepository.GetTenantOverrideAsync(
            command.MeterCode,
            cancellationToken);

        if (existing is null)
        {
            return Result.Failure(Error.NotFound("QuotaOverride", $"{command.TenantId}:{command.MeterCode}"));
        }

        quotaRepository.Remove(existing);
        await quotaRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
