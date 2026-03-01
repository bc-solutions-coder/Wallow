using Foundry.Billing.Application.DTOs;
using Foundry.Billing.Application.Interfaces;
using Foundry.Billing.Application.Mappings;
using Foundry.Billing.Domain.Entities;
using Foundry.Billing.Domain.Identity;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Billing.Application.Queries.GetSubscriptionById;

public sealed class GetSubscriptionByIdHandler(ISubscriptionRepository subscriptionRepository)
{
    public async Task<Result<SubscriptionDto>> Handle(
        GetSubscriptionByIdQuery query,
        CancellationToken cancellationToken)
    {
        SubscriptionId subscriptionId = SubscriptionId.Create(query.SubscriptionId);
        Subscription? subscription = await subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure<SubscriptionDto>(Error.NotFound("Subscription", query.SubscriptionId));
        }

        return Result.Success(subscription.ToDto());
    }
}
