using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Foundry.Billing.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for BillingDbContext to enable EF Core migrations.
/// Only used at design-time by dotnet ef commands.
/// </summary>
public class BillingDbContextFactory : IDesignTimeDbContextFactory<BillingDbContext>
{
    public BillingDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<BillingDbContext> optionsBuilder = new DbContextOptionsBuilder<BillingDbContext>();

        // Use a placeholder connection string for design-time
        optionsBuilder.UseNpgsql("Host=localhost;Database=foundry;Username=postgres;Password=postgres");

        // Create a mock tenant context for design-time
        DesignTimeTenantContext mockTenantContext = new DesignTimeTenantContext();

        return new BillingDbContext(optionsBuilder.Options, mockTenantContext);
    }
}
