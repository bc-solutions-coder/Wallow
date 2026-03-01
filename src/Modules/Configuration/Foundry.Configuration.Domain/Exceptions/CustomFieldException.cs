using Foundry.Shared.Kernel.Domain;

namespace Foundry.Configuration.Domain.Exceptions;

public class CustomFieldException : BusinessRuleException
{
    public CustomFieldException(string message)
        : base("Configuration.CustomField", message)
    {
    }

    public CustomFieldException()
    {
    }

    public CustomFieldException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
