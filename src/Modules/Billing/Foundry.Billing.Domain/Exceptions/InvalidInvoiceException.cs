using Foundry.Shared.Kernel.Domain;

namespace Foundry.Billing.Domain.Exceptions;

public sealed class InvalidInvoiceException : DomainException
{
    public InvalidInvoiceException(string message)
        : base("Billing.InvalidInvoice", message) { }

    public InvalidInvoiceException()
    {
    }

    public InvalidInvoiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
