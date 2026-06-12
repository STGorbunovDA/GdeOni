using GdeOni.Domain.Aggregates.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

/// <summary>
/// D33. Вложения в тикеты поддержки. Каскадное удаление через FK на
/// support_tickets (тикет удалён → его файлы удалены из БД; файлы
/// в MinIO чистятся MinioOrphanCleanupService по обычному пути).
/// </summary>
public sealed class SupportTicketAttachmentConfiguration : IEntityTypeConfiguration<SupportTicketAttachment>
{
    public void Configure(EntityTypeBuilder<SupportTicketAttachment> builder)
    {
        builder.ToTable("support_ticket_attachments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.TicketId)
            .HasColumnName("ticket_id")
            .IsRequired();

        builder.Property(x => x.OriginalFileName)
            .HasColumnName("original_file_name")
            .HasMaxLength(SupportTicketAttachment.MaxFileNameLength)
            .IsRequired();

        builder.Property(x => x.Bucket)
            .HasColumnName("bucket")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.SizeBytes)
            .HasColumnName("size_bytes")
            .IsRequired();

        builder.Property(x => x.UploadedAtUtc)
            .HasColumnName("uploaded_at_utc")
            .IsRequired();

        // Чтение списка вложений тикета — WHERE ticket_id = X.
        builder.HasIndex(x => x.TicketId)
            .HasDatabaseName("ix_support_ticket_attachments_ticket_id");

        // Storage key уникален — гарантирует что один файл в MinIO
        // не привязан к двум разным записям одновременно (важно для
        // orphan cleanup, который полагается на storage key).
        builder.HasIndex(x => x.StorageKey)
            .IsUnique()
            .HasDatabaseName("ux_support_ticket_attachments_storage_key");
    }
}
