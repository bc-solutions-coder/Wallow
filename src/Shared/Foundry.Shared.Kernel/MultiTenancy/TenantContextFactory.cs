using Foundry.Shared.Kernel.Identity;

namespace Foundry.Shared.Kernel.MultiTenancy;

public sealed class TenantContextFactory : ITenantContextFactory
{
    private readonly ITenantContext _tenantContext;

    public TenantContextFactory(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public IDisposable CreateScope(TenantId tenantId)
    {
        _tenantContext.SetTenant(tenantId);
        return new TenantContextScope(_tenantContext);
    }

    private sealed class TenantContextScope : IDisposable
    {
        private readonly ITenantContext _context;

        public TenantContextScope(ITenantContext context)
        {
            _context = context;
        }

        public void Dispose()
        {
            _context.Clear();
        }
    }
}
