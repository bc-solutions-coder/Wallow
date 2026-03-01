using Foundry.Billing.Application.Interfaces;
using Foundry.Billing.Domain.Entities;
using Foundry.Shared.Contracts.Billing;
using Microsoft.Extensions.Logging;

namespace Foundry.Billing.Infrastructure.Services;

public sealed partial class SubscriptionQueryService : ISubscriptionQueryService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ILogger<SubscriptionQueryService> _logger;

    public SubscriptionQueryService(
        ISubscriptionRepository subscriptionRepository,
        ILogger<SubscriptionQueryService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _logger = logger;
    }

    public async Task<string?> GetActivePlanCodeAsync(Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            Subscription? subscription = await _subscriptionRepository.GetActiveByUserIdAsync(tenantId, ct);

            if (subscription is null)
            {
                LogNoActiveSubscription(_logger, tenantId);
                return null;
            }

            return subscription.PlanName;
        }
        catch (Exception ex)
        {
            LogGetActivePlanFailed(_logger, ex, tenantId);
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "No active subscription found for tenant {TenantId}")]
    private static partial void LogNoActiveSubscription(ILogger logger, Guid tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to get active plan code for tenant {TenantId}")]
    private static partial void LogGetActivePlanFailed(ILogger logger, Exception ex, Guid tenantId);
}
