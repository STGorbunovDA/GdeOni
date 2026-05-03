using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

public sealed class DeceasedMediaConfiguration : IEntityTypeConfiguration<DeceasedMedia>
{
    public void Configure(EntityTypeBuilder<DeceasedMedia> builder)
    {
        builder.ToTable("deceased_media");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.DeceasedId)
            .HasColumnName("deceased_id")
            .IsRequired();

        builder.Property(x => x.UploadedByUserId)
            .HasColumnName("uploaded_by_user_id")
            .IsRequired();

        builder.Property(x => x.Kind)
            .HasColumnName("kind")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.OriginalFileName)
            .HasColumnName("original_file_name")
            .HasMaxLength(DeceasedMedia.MaxOriginalFileNameLength)
            .IsRequired();

        builder.Property(x => x.Bucket)
            .HasColumnName("bucket")
            .HasMaxLength(DeceasedMedia.MaxBucketLength)
            .IsRequired();

        builder.Property(x => x.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(DeceasedMedia.MaxStorageKeyLength)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(DeceasedMedia.MaxContentTypeLength)
            .IsRequired();

        builder.Property(x => x.SizeBytes)
            .HasColumnName("size_bytes")
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(DeceasedMedia.MaxDescriptionLength);

        builder.Property(x => x.IsMainPhoto)
            .HasColumnName("is_main_photo")
            .IsRequired();

        builder.Property(x => x.ModerationStatus)
            .HasColumnName("moderation_status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(x => x.DeceasedId)
            .HasDatabaseName("ix_deceased_media_deceased_id");

        builder.HasIndex(x => x.Kind)
            .HasDatabaseName("ix_deceased_media_kind");

        builder.HasIndex(x => x.UploadedByUserId)
            .HasDatabaseName("ix_deceased_media_uploaded_by_user_id");

        builder.HasIndex(x => x.StorageKey)
            .IsUnique()
            .HasDatabaseName(DbConstraints.UxDeceasedMediaStorageKey);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
