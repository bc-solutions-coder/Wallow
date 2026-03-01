using Foundry.Shared.Kernel.Identity;
using Foundry.Shared.Kernel.MultiTenancy;

namespace Foundry.Billing.Infrastructure.Persistence;

/// <summary>
/// Mock ITenantContext for design-time migrations.
/// Returns a placeholder TenantId that is never used at runtime.
/// </summary>
internal sealed class DesignTimeTenantContext : ITenantContext
{
    public TenantId TenantId => new(Guid.Parse("00000000-0000-0000-0000-000000000000"));
    public string TenantName => "design-time";
    public string Region => RegionConfiguration.PrimaryRegion;
    public bool IsResolved => true;

    public void SetTenant(TenantId tenantId, string tenantName = "", string region = RegionConfiguration.PrimaryRegion)
    {
        // No-op for design-time
    }

    public void Clear()
    {
        // No-op for design-time
    }
}
