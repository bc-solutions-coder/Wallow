using System.Linq.Expressions;
using Foundry.Billing.Domain.Entities;
using Foundry.Billing.Domain.Metering.Entities;
using Foundry.Shared.Kernel.Identity;
using Foundry.Shared.Kernel.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Foundry.Billing.Infrastructure.Persistence;

public sealed class BillingDbContext : DbContext
{
    // Store tenant ID as a field for query filter access
    // EF Core can properly translate member access to DbContext fields
#pragma warning disable IDE0052 // Accessed via expression tree in OnModelCreating query filters
    private readonly TenantId _tenantId;
#pragma warning restore IDE0052

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<MeterDefinition> MeterDefinitions => Set<MeterDefinition>();
    public DbSet<QuotaDefinition> QuotaDefinitions => Set<QuotaDefinition>();
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();

    public BillingDbContext(DbContextOptions<BillingDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantId = tenantContext.TenantId;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("billing");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                ParameterExpression parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                MemberExpression property = System.Linq.Expressions.Expression.Property(parameter, nameof(ITenantScoped.TenantId));

                // Access the _tenantId field via 'this' reference
                ConstantExpression contextExpression = System.Linq.Expressions.Expression.Constant(this);
                MemberExpression tenantIdField = System.Linq.Expressions.Expression.Field(
                    contextExpression,
                    typeof(BillingDbContext).GetField("_tenantId",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!);

                BinaryExpression equals = System.Linq.Expressions.Expression.Equal(property, tenantIdField);
                LambdaExpression lambda = System.Linq.Expressions.Expression.Lambda(equals, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
