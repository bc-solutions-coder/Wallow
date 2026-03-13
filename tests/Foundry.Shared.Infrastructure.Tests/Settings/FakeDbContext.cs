using Foundry.Shared.Infrastructure.Core.Persistence;
using Foundry.Shared.Kernel.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Foundry.Shared.Infrastructure.Tests.Settings;

// Minimal concrete DbContext used only as a type parameter discriminator in generic tests
public sealed class FakeDbContext(DbContextOptions<FakeDbContext> options, ITenantContext tenantContext)
    : TenantAwareDbContext<FakeDbContext>(options, tenantContext);
