using Foundry.Billing.Application.DTOs;
using Foundry.Billing.Application.Interfaces;
using Foundry.Billing.Application.Mappings;
using Foundry.Billing.Domain.Entities;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Billing.Application.Queries.GetSubscriptionsByUserId;

public sealed class GetSubscriptionsByUserIdHandler(ISubscriptionRepository subscriptionRepository)
{
    public async Task<Result<IReadOnlyList<SubscriptionDto>>> Handle(
        GetSubscriptionsByUserIdQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Subscription> subscriptions = await subscriptionRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        List<SubscriptionDto> dtos = subscriptions.Select(s => s.ToDto()).ToList();
        return Result.Success<IReadOnlyList<SubscriptionDto>>(dtos);
    }
}
