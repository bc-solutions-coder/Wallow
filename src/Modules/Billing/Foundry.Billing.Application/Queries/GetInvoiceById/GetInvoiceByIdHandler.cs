using Foundry.Billing.Application.DTOs;
using Foundry.Billing.Application.Interfaces;
using Foundry.Billing.Application.Mappings;
using Foundry.Billing.Domain.Entities;
using Foundry.Billing.Domain.Identity;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Billing.Application.Queries.GetInvoiceById;

public sealed class GetInvoiceByIdHandler(IInvoiceRepository invoiceRepository)
{
    public async Task<Result<InvoiceDto>> Handle(
        GetInvoiceByIdQuery query,
        CancellationToken cancellationToken)
    {
        InvoiceId invoiceId = InvoiceId.Create(query.InvoiceId);
        Invoice? invoice = await invoiceRepository.GetByIdWithLineItemsAsync(invoiceId, cancellationToken);

        if (invoice is null)
        {
            return Result.Failure<InvoiceDto>(Error.NotFound("Invoice", query.InvoiceId));
        }

        return Result.Success(invoice.ToDto());
    }
}
