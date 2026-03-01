namespace Foundry.Shared.Contracts.Billing;

public interface ISubscriptionQueryService
{
    Task<string?> GetActivePlanCodeAsync(Guid tenantId, CancellationToken ct = default);
}
