using Foundry.Shared.Kernel.Identity.Authorization;

namespace Foundry.Identity.Infrastructure.Authorization;

public static class RolePermissionMapping
{
    private static readonly Dictionary<string, PermissionType[]> _rolePermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["admin"] = Enum.GetValues<PermissionType>(),
        ["manager"] = new[]
        {
            PermissionType.UsersRead,
            PermissionType.BillingRead,
            PermissionType.OrganizationsRead,
            PermissionType.OrganizationsManageMembers,
            PermissionType.ApiKeysRead,
            PermissionType.ApiKeysCreate,
            PermissionType.ApiKeysUpdate,
            PermissionType.ApiKeysDelete,
            PermissionType.SsoRead,
            PermissionType.ConfigurationManage,
        },
        ["user"] = new[]
        {
            PermissionType.OrganizationsRead,
        }
    };

    public static IEnumerable<PermissionType> GetPermissions(IEnumerable<string> roles)
    {
        return roles
            .Where(r => _rolePermissions.ContainsKey(r))
            .SelectMany(r => _rolePermissions[r])
            .Distinct();
    }
}
