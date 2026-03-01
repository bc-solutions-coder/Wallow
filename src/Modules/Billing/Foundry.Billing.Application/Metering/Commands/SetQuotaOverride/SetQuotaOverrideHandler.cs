using Foundry.Billing.Application.Metering.Interfaces;
using Foundry.Billing.Domain.Metering.Entities;
using Foundry.Shared.Kernel.Identity;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Billing.Application.Metering.Commands.SetQuotaOverride;

public sealed class SetQuotaOverrideHandler(
    IQuotaDefinitionRepository quotaRepository,
    IMeterDefinitionRepository meterRepository)
{
    public async Task<Result> Handle(SetQuotaOverrideCommand command, CancellationToken cancellationToken)
    {
        MeterDefinition? meter = await meterRepository.GetByCodeAsync(command.MeterCode, cancellationToken);
        if (meter is null)
        {
            return Result.Failure(Error.NotFound("MeterDefinition", command.MeterCode));
        }

        QuotaDefinition? existing = await quotaRepository.GetTenantOverrideAsync(
            command.MeterCode,
            cancellationToken);

        if (existing is not null)
        {
            existing.UpdateLimit(command.Limit, command.OnExceeded, Guid.Empty);
        }
        else
        {
            TenantId tenantId = TenantId.Create(command.TenantId);
            QuotaDefinition quota = QuotaDefinition.CreateTenantOverride(
                command.MeterCode,
                tenantId,
                command.Limit,
                command.Period,
                command.OnExceeded);

            quotaRepository.Add(quota);
        }

        await quotaRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
