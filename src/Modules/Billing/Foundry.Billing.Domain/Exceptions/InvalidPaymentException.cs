using Foundry.Shared.Kernel.Domain;

namespace Foundry.Billing.Domain.Exceptions;

public sealed class InvalidPaymentException : DomainException
{
    public InvalidPaymentException(string message)
        : base("Billing.InvalidPayment", message) { }

    public InvalidPaymentException()
    {
    }

    public InvalidPaymentException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
