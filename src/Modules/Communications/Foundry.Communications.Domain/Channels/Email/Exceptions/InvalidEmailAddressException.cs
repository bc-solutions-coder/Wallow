using Foundry.Shared.Kernel.Domain;

namespace Foundry.Communications.Domain.Channels.Email.Exceptions;

public sealed class InvalidEmailAddressException : DomainException
{
    public InvalidEmailAddressException(string message)
        : base("Email.InvalidEmailAddress", message)
    {
    }

    public InvalidEmailAddressException()
    {
    }

    public InvalidEmailAddressException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
