using Microsoft.AspNetCore.Identity;

namespace Foundry.Identity.Domain.Entities;

public class FoundryRole : IdentityRole<Guid>
{
    public Guid TenantId { get; set; }
}
