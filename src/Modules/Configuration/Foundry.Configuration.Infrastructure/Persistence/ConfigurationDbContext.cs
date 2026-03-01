using System.Linq.Expressions;
using Foundry.Configuration.Domain.Entities;
using Foundry.Shared.Kernel.Identity;
using Foundry.Shared.Kernel.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Foundry.Configuration.Infrastructure.Persistence;

public sealed class ConfigurationDbContext : DbContext
{
    // Store tenant ID as a field for query filter access
    // EF Core can properly translate member access to DbContext fields
#pragma warning disable IDE0052 // Used by EF Core query filter via reflection
    private readonly TenantId _tenantId;
#pragma warning restore IDE0052

    public DbSet<CustomFieldDefinition> CustomFieldDefinitions => Set<CustomFieldDefinition>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<FeatureFlagOverride> FeatureFlagOverrides => Set<FeatureFlagOverride>();

    public ConfigurationDbContext(
        DbContextOptions<ConfigurationDbContext> options,
        ITenantContext tenantContext) : base(options)
    {
        _tenantId = tenantContext.TenantId;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("configuration");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConfigurationDbContext).Assembly);

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
                    typeof(ConfigurationDbContext).GetField("_tenantId",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!);

                BinaryExpression equals = System.Linq.Expressions.Expression.Equal(property, tenantIdField);
                LambdaExpression lambda = System.Linq.Expressions.Expression.Lambda(equals, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
