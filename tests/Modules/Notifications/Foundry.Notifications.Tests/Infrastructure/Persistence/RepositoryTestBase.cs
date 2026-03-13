using Foundry.Notifications.Infrastructure.Persistence;
using Foundry.Shared.Kernel.Identity;
using Foundry.Shared.Kernel.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Foundry.Notifications.Tests.Infrastructure.Persistence;

public abstract class RepositoryTestBase : IDisposable
{
    private static readonly TenantId _testTenantId = TenantId.New();
    private bool _disposed;

    protected static TenantId TestTenantId => _testTenantId;

    protected NotificationsDbContext Context { get; }

    protected RepositoryTestBase()
    {
        ITenantContext tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(_testTenantId);

        DbContextOptions<NotificationsDbContext> options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Context = new NotificationsDbContext(options, tenantContext);
    }

    protected void SetTenantId<TEntity>(TEntity entity) where TEntity : class, ITenantScoped
    {
        Context.Entry(entity).Property(nameof(ITenantScoped.TenantId)).CurrentValue = _testTenantId;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Context.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
