using Foundry.Billing.Domain.Entities;
using Foundry.Billing.Domain.ValueObjects;

namespace Foundry.Tests.Common.Builders;

/// <summary>
/// Fluent builder for creating Invoice test data.
/// </summary>
public class InvoiceBuilder
{
    private Guid _userId = Guid.NewGuid();
    private string _invoiceNumber = $"INV-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
    private string _currency = "USD";
    private Guid _createdBy = Guid.NewGuid();
    private DateTime? _dueDate;
    private readonly List<(string Description, decimal Amount, int Quantity)> _lineItems = [];
    private bool _issued;
    private bool _paid;
    private bool _overdue;
    private bool _cancelled;

    public InvoiceBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public InvoiceBuilder WithInvoiceNumber(string number)
    {
        _invoiceNumber = number;
        return this;
    }

    public InvoiceBuilder WithCurrency(string currency)
    {
        _currency = currency;
        return this;
    }

    public InvoiceBuilder WithCreatedBy(Guid createdBy)
    {
        _createdBy = createdBy;
        return this;
    }

    public InvoiceBuilder WithDueDate(DateTime dueDate)
    {
        _dueDate = dueDate;
        return this;
    }

    public InvoiceBuilder WithDueDateInDays(int days)
    {
        _dueDate = DateTime.UtcNow.AddDays(days);
        return this;
    }

    public InvoiceBuilder WithLineItem(string description, decimal amount, int quantity = 1)
    {
        _lineItems.Add((description, amount, quantity));
        return this;
    }

    public InvoiceBuilder WithDefaultLineItem()
    {
        _lineItems.Add(("Default Service", 100m, 1));
        return this;
    }

    public InvoiceBuilder AsIssued()
    {
        _issued = true;
        return this;
    }

    public InvoiceBuilder AsPaid()
    {
        _issued = true;
        _paid = true;
        return this;
    }

    public InvoiceBuilder AsOverdue()
    {
        _issued = true;
        _overdue = true;
        return this;
    }

    public InvoiceBuilder AsCancelled()
    {
        _cancelled = true;
        return this;
    }

    public Invoice Build()
    {
        Invoice invoice = Invoice.Create(_userId, _invoiceNumber, _currency, _createdBy, _dueDate);

        // Add line items
        foreach ((string? description, decimal amount, int quantity) in _lineItems)
        {
            invoice.AddLineItem(description, Money.Create(amount, _currency), quantity, _createdBy);
        }

        // Apply state transitions
        if (_issued || _paid || _overdue)
        {
            // Must have at least one line item to issue
            if (_lineItems.Count == 0)
            {
                invoice.AddLineItem("Default Item", Money.Create(100, _currency), 1, _createdBy);
            }
            invoice.Issue(_createdBy);
        }

        if (_overdue)
        {
            invoice.MarkAsOverdue(_createdBy);
        }

        if (_paid)
        {
            invoice.MarkAsPaid(Guid.NewGuid(), _createdBy);
        }

        if (_cancelled)
        {
            invoice.Cancel(_createdBy);
        }

        // Clear domain events from setup so tests can assert on their own events
        invoice.ClearDomainEvents();

        return invoice;
    }

    /// <summary>
    /// Creates a new builder instance.
    /// </summary>
    public static InvoiceBuilder Create() => new();
}
