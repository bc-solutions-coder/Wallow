using System.Linq.Expressions;
using Foundry.Communications.Domain.Announcements.Entities;
using Foundry.Communications.Domain.Channels.Email.Entities;
using Foundry.Communications.Domain.Channels.InApp.Entities;
using Foundry.Shared.Kernel.Identity;
using Foundry.Shared.Kernel.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Foundry.Communications.Infrastructure.Persistence;

public sealed class CommunicationsDbContext : DbContext
{
    // Store tenant ID as a field for query filter access
    // EF Core can properly translate member access to DbContext fields
#pragma warning disable IDE0052 // Accessed via expression tree in OnModelCreating query filters
    private readonly TenantId _tenantId;
#pragma warning restore IDE0052

    // Email
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<EmailPreference> EmailPreferences => Set<EmailPreference>();

    // InApp Notifications
    public DbSet<Notification> Notifications => Set<Notification>();

    // Announcements
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AnnouncementDismissal> AnnouncementDismissals => Set<AnnouncementDismissal>();
    public DbSet<ChangelogEntry> ChangelogEntries => Set<ChangelogEntry>();
    public DbSet<ChangelogItem> ChangelogItems => Set<ChangelogItem>();

    public CommunicationsDbContext(
        DbContextOptions<CommunicationsDbContext> options,
        ITenantContext tenantContext) : base(options)
    {
        _tenantId = tenantContext.TenantId;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("communications");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommunicationsDbContext).Assembly);

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "e");
                MemberExpression property = Expression.Property(parameter, nameof(ITenantScoped.TenantId));

                ConstantExpression contextExpression = Expression.Constant(this);
                MemberExpression tenantIdField = Expression.Field(
                    contextExpression,
                    typeof(CommunicationsDbContext).GetField("_tenantId",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!);

                BinaryExpression equals = Expression.Equal(property, tenantIdField);
                LambdaExpression lambda = Expression.Lambda(equals, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
