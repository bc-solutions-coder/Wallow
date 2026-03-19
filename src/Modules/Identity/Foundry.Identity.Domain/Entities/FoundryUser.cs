using Foundry.Shared.Kernel.Domain;
using Microsoft.AspNetCore.Identity;

namespace Foundry.Identity.Domain.Entities;

public sealed class FoundryUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? DeactivatedAt { get; private set; }

    private FoundryUser() { } // EF Core

    public static FoundryUser Create(
        Guid tenantId,
        string firstName,
        string lastName,
        string email,
        TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new BusinessRuleException(
                "Identity.FirstNameRequired",
                "First name cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new BusinessRuleException(
                "Identity.LastNameRequired",
                "Last name cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new BusinessRuleException(
                "Identity.EmailRequired",
                "Email cannot be empty");
        }

        FoundryUser user = new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            CreatedAt = timeProvider.GetUtcNow()
        };

        return user;
    }
}
