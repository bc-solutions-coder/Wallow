using System.Linq.Expressions;
using Foundry.Shared.Kernel.Identity;
using Foundry.Shared.Kernel.MultiTenancy;
using Foundry.Storage.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Foundry.Storage.Infrastructure.Persistence;

public sealed class StorageDbContext : DbContext
{
    // Store tenant ID as a field for query filter access
    // EF Core accesses this field via expression tree for query filters
#pragma warning disable IDE0052 // Used via reflection in OnModelCreating
    private readonly TenantId _tenantId;
#pragma warning restore IDE0052

    public DbSet<StorageBucket> Buckets => Set<StorageBucket>();
    public DbSet<StoredFile> Files => Set<StoredFile>();

    public StorageDbContext(DbContextOptions<StorageDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantId = tenantContext.TenantId;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("storage");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StorageDbContext).Assembly);

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
                    typeof(StorageDbContext).GetField("_tenantId",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!);

                BinaryExpression equals = System.Linq.Expressions.Expression.Equal(property, tenantIdField);
                LambdaExpression lambda = System.Linq.Expressions.Expression.Lambda(equals, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
