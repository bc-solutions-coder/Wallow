using Foundry.Billing.Application.DTOs;
using Foundry.Billing.Application.Interfaces;
using Foundry.Billing.Application.Mappings;
using Foundry.Billing.Domain.Entities;
using Foundry.Billing.Domain.Identity;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Billing.Application.Queries.GetPaymentById;

public sealed class GetPaymentByIdHandler(IPaymentRepository paymentRepository)
{
    public async Task<Result<PaymentDto>> Handle(
        GetPaymentByIdQuery query,
        CancellationToken cancellationToken)
    {
        PaymentId paymentId = PaymentId.Create(query.PaymentId);
        Payment? payment = await paymentRepository.GetByIdAsync(paymentId, cancellationToken);

        if (payment is null)
        {
            return Result.Failure<PaymentDto>(Error.NotFound("Payment", query.PaymentId));
        }

        return Result.Success(payment.ToDto());
    }
}
