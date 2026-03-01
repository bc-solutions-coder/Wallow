using Foundry.Billing.Application.DTOs;
using Foundry.Billing.Application.Interfaces;
using Foundry.Billing.Application.Mappings;
using Foundry.Billing.Domain.Entities;
using Foundry.Shared.Kernel.Results;

namespace Foundry.Billing.Application.Queries.GetAllInvoices;

public sealed class GetAllInvoicesHandler(IInvoiceRepository invoiceRepository)
{
    public async Task<Result<IReadOnlyList<InvoiceDto>>> Handle(
        GetAllInvoicesQuery _,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Invoice> invoices = await invoiceRepository.GetAllAsync(cancellationToken);
        List<InvoiceDto> dtos = invoices.Select(i => i.ToDto()).ToList();
        return Result.Success<IReadOnlyList<InvoiceDto>>(dtos);
    }
}
