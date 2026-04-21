using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

public sealed class DeceasedPhotoConfiguration : IEntityTypeConfiguration<DeceasedPhoto>
{
    public void Configure(EntityTypeBuilder<DeceasedPhoto> builder)
    {
        builder.ToTable("deceased_photos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property<Guid>("deceased_id")
            .HasColumnName("deceased_id")
            .IsRequired();

        builder.Property(x => x.Url)
            .HasColumnName("url")
            .HasMaxLength(DeceasedPhoto.MaxUrlLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(DeceasedPhoto.MaxDescriptionLength);

        builder.Property(x => x.IsPrimary)
            .HasColumnName("is_primary")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(x => x.AddedByUserId)
            .HasColumnName("added_by_user_id")
            .IsRequired();

        builder.Property(x => x.ModerationStatus)
            .HasColumnName("moderation_status")
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.AddedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("deceased_id")
            .HasDatabaseName("ix_deceased_photos_deceased_id");

        builder.HasIndex(x => x.AddedByUserId)
            .HasDatabaseName("ix_deceased_photos_added_by_user_id");
    }
}