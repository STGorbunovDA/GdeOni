using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

public sealed class DeceasedMemoryEntryConfiguration : IEntityTypeConfiguration<DeceasedMemoryEntry>
{
    public void Configure(EntityTypeBuilder<DeceasedMemoryEntry> builder)
    {
        builder.ToTable("deceased_memory_entries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property<Guid>("deceased_id")
            .HasColumnName("deceased_id")
            .IsRequired();

        builder.Property(x => x.Text)
            .HasColumnName("text")
            .HasMaxLength(DeceasedMemoryEntry.MaxTextLength)
            .IsRequired();

        builder.Property(x => x.AuthorUserId)
            .HasColumnName("author_user_id");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(x => x.ModerationStatus)
            .HasColumnName("moderation_status")
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.AuthorUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex("deceased_id")
            .HasDatabaseName("ix_memory_entries_deceased_id");

        builder.HasIndex(x => x.AuthorUserId)
            .HasDatabaseName("ix_memory_entries_author_user_id");
    }
}