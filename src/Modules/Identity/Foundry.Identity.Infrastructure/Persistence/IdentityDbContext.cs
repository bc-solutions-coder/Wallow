using System.Linq.Expressions;
using Foundry.Identity.Domain.Entities;
using Foundry.Shared.Kernel.Identity;
using Foundry.Shared.Kernel.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Foundry.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext : DbContext
{
    // Store tenant ID as a field for query filter access
    // EF Core can properly translate member access to DbContext fields
#pragma warning disable IDE0052 // Accessed via expression tree in OnModelCreating query filters
    private readonly TenantId _tenantId;
#pragma warning restore IDE0052

    public DbSet<ServiceAccountMetadata> ServiceAccountMetadata => Set<ServiceAccountMetadata>();
    public DbSet<ApiScope> ApiScopes => Set<ApiScope>();
    public DbSet<SsoConfiguration> SsoConfigurations => Set<SsoConfiguration>();
    public DbSet<ScimConfiguration> ScimConfigurations => Set<ScimConfiguration>();
    public DbSet<ScimSyncLog> ScimSyncLogs => Set<ScimSyncLog>();

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantId = tenantContext.TenantId;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "e");
                MemberExpression property = Expression.Property(parameter, nameof(ITenantScoped.TenantId));

                // Access the _tenantId field via 'this' reference
                ConstantExpression contextExpression = Expression.Constant(this);
                MemberExpression tenantIdField = Expression.Field(
                    contextExpression,
                    typeof(IdentityDbContext).GetField("_tenantId",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!);

                BinaryExpression equals = Expression.Equal(property, tenantIdField);
                LambdaExpression lambda = Expression.Lambda(equals, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
