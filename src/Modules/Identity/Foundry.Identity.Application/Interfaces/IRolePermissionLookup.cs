using Foundry.Shared.Kernel.Identity.Authorization;

namespace Foundry.Identity.Application.Interfaces;

public interface IRolePermissionLookup
{
    IReadOnlyCollection<PermissionType> GetPermissions(IEnumerable<string> roles);
}
