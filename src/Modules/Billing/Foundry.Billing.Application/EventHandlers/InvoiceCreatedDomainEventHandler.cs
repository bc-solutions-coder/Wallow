using Foundry.Billing.Application.Interfaces;
using Foundry.Billing.Application.Telemetry;
using Foundry.Billing.Domain.Entities;
using Foundry.Billing.Domain.Events;
using Foundry.Billing.Domain.Identity;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Foundry.Billing.Application.EventHandlers;

public sealed partial class InvoiceCreatedDomainEventHandler
{
    private const int DefaultDueDateDays = 30;

    public static async Task HandleAsync(
        InvoiceCreatedDomainEvent domainEvent,
        IInvoiceRepository invoiceRepository,
        IMessageBus bus,
        ILogger<InvoiceCreatedDomainEventHandler> logger,
        CancellationToken cancellationToken)
    {
        LogHandlingInvoiceCreated(logger, domainEvent.InvoiceId);

        // Enrich with additional data
        Invoice? invoice = await invoiceRepository.GetByIdAsync(
            InvoiceId.Create(domainEvent.InvoiceId), cancellationToken);

        // Publish integration event for other modules
        await bus.PublishAsync(new Foundry.Shared.Contracts.Billing.Events.InvoiceCreatedEvent
        {
            InvoiceId = domainEvent.InvoiceId,
            TenantId = invoice?.TenantId.Value ?? Guid.Empty,
            UserId = domainEvent.UserId,
            UserEmail = string.Empty, // Would be enriched from Identity module in production
            InvoiceNumber = invoice?.InvoiceNumber ?? string.Empty,
            Amount = domainEvent.TotalAmount,
            Currency = domainEvent.Currency,
            DueDate = invoice?.DueDate ?? DateTime.UtcNow.AddDays(DefaultDueDateDays)
        });

        string status = invoice?.Status.ToString() ?? "Unknown";
        string currency = domainEvent.Currency;

        BillingModuleTelemetry.InvoicesCreatedTotal.Add(1,
            new KeyValuePair<string, object?>("status", status),
            new KeyValuePair<string, object?>("currency", currency));

        BillingModuleTelemetry.InvoiceAmount.Record((double)domainEvent.TotalAmount,
            new KeyValuePair<string, object?>("status", status),
            new KeyValuePair<string, object?>("currency", currency));

        LogPublishedInvoiceCreated(logger, domainEvent.InvoiceId);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Handling InvoiceCreatedDomainEvent for Invoice {InvoiceId}")]
    private static partial void LogHandlingInvoiceCreated(ILogger logger, Guid invoiceId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Published InvoiceCreatedEvent for Invoice {InvoiceId}")]
    private static partial void LogPublishedInvoiceCreated(ILogger logger, Guid invoiceId);
}
