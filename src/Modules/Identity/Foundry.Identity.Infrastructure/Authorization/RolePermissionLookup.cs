using Foundry.Identity.Application.Interfaces;
using Foundry.Shared.Kernel.Identity.Authorization;

namespace Foundry.Identity.Infrastructure.Authorization;

public sealed class RolePermissionLookup : IRolePermissionLookup
{
    public IReadOnlyCollection<PermissionType> GetPermissions(IEnumerable<string> roles)
    {
        return RolePermissionMapping.GetPermissions(roles).ToArray();
    }
}
