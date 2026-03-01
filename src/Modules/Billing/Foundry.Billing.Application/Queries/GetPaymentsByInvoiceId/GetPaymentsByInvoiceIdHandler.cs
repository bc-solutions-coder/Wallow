using Foundry.Billing.Application.DTOs;
using Foundry.Billing.Application.Interfaces;
using Foundry.Billing.Application.Mappings;
using Foundry.Billing.Domain.Entities;
using Foundry.Billing.Domain.Identity;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Billing.Application.Queries.GetPaymentsByInvoiceId;

public sealed class GetPaymentsByInvoiceIdHandler(IPaymentRepository paymentRepository)
{
    public async Task<Result<IReadOnlyList<PaymentDto>>> Handle(
        GetPaymentsByInvoiceIdQuery query,
        CancellationToken cancellationToken)
    {
        InvoiceId invoiceId = InvoiceId.Create(query.InvoiceId);
        IReadOnlyList<Payment> payments = await paymentRepository.GetByInvoiceIdAsync(invoiceId, cancellationToken);
        List<PaymentDto> dtos = payments.Select(p => p.ToDto()).ToList();
        return Result.Success<IReadOnlyList<PaymentDto>>(dtos);
    }
}
