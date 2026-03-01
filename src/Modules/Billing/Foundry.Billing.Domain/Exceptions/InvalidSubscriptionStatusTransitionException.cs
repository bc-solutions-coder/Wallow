using Foundry.Shared.Kernel.Domain;

namespace Foundry.Billing.Domain.Exceptions;

public sealed class InvalidSubscriptionStatusTransitionException : DomainException
{
    public InvalidSubscriptionStatusTransitionException(string fromStatus, string toStatus)
        : base("Billing.InvalidSubscriptionStatusTransition",
            $"Cannot transition subscription from '{fromStatus}' to '{toStatus}'")
    { }

    public InvalidSubscriptionStatusTransitionException()
    {
    }

    public InvalidSubscriptionStatusTransitionException(string message) : base(message)
    {
    }

    public InvalidSubscriptionStatusTransitionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
