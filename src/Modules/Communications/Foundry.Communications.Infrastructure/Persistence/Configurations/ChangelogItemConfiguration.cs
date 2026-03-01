using Foundry.Communications.Domain.Announcements.Entities;
using Foundry.Communications.Domain.Announcements.Identity;
using Foundry.Shared.Kernel.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Foundry.Communications.Infrastructure.Persistence.Configurations;

public sealed class ChangelogItemConfiguration : IEntityTypeConfiguration<ChangelogItem>
{
    public void Configure(EntityTypeBuilder<ChangelogItem> builder)
    {
        builder.ToTable("changelog_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .HasConversion(new StronglyTypedIdConverter<ChangelogItemId>())
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(i => i.EntryId)
            .HasConversion(new StronglyTypedIdConverter<ChangelogEntryId>())
            .HasColumnName("entry_id")
            .IsRequired();

        builder.Property(i => i.Description)
            .HasColumnName("description")
            .IsRequired();

        builder.Property(i => i.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.HasIndex(i => i.EntryId);
    }
}
