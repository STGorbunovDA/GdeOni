using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

public sealed class TrackedDeceasedConfiguration : IEntityTypeConfiguration<TrackedDeceased>
{
    public void Configure(EntityTypeBuilder<TrackedDeceased> builder)
    {
        builder.ToTable("tracked_deceased");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property<Guid>("user_id")
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.DeceasedId)
            .HasColumnName("deceased_id")
            .IsRequired();

        builder.Property(x => x.RelationshipType)
            .HasColumnName("relationship_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.PersonalNotes)
            .HasColumnName("personal_notes")
            .HasMaxLength(TrackedDeceased.MaxPersonalNotesLength);

        builder.Property(x => x.NotifyOnDeathAnniversary)
            .HasColumnName("notify_on_death_anniversary")
            .IsRequired();

        builder.Property(x => x.NotifyOnBirthAnniversary)
            .HasColumnName("notify_on_birth_anniversary")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.TrackedAtUtc)
            .HasColumnName("tracked_at_utc")
            .IsRequired();

        builder.HasIndex("user_id")
            .HasDatabaseName("ix_tracked_deceased_user_id");

        builder.HasIndex(x => x.DeceasedId)
            .HasDatabaseName("ix_tracked_deceased_deceased_id");

        builder.HasOne<Deceased>()
            .WithMany()
            .HasForeignKey(x => x.DeceasedId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex("user_id", nameof(TrackedDeceased.DeceasedId))
            .IsUnique()
            .HasDatabaseName("ux_tracked_deceased_user_id_deceased_id");
    }
}