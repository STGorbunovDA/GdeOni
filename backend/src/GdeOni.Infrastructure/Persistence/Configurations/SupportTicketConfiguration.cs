using GdeOni.Domain.Aggregates.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

public sealed class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("support_tickets");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id");

        builder.Property(x => x.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Severity)
            .HasColumnName("severity")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(SupportTicket.MaxTitleLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(SupportTicket.MaxDescriptionLength)
            .IsRequired();

        builder.Property(x => x.Details)
            .HasColumnName("details")
            .HasColumnType("jsonb");

        builder.Property(x => x.ResolutionNote)
            .HasColumnName("resolution_note")
            .HasMaxLength(SupportTicket.MaxResolutionNoteLength);

        builder.Property(x => x.ResolvedByUserId)
            .HasColumnName("resolved_by_user_id");

        builder.Property(x => x.ResolvedAtUtc)
            .HasColumnName("resolved_at_utc");

        builder.Property(x => x.AcceptedByUser)
            .HasColumnName("accepted_by_user")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.AcceptedByUserAtUtc)
            .HasColumnName("accepted_by_user_at_utc");

        builder.Property(x => x.LastUserReply)
            .HasColumnName("last_user_reply")
            .HasMaxLength(SupportTicket.MaxUserReplyLength);

        builder.Property(x => x.LastUserReplyAtUtc)
            .HasColumnName("last_user_reply_at_utc");

        builder.Property(x => x.ReopenedCount)
            .HasColumnName("reopened_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        // FK на users — SetNull при удалении: история инцидентов
        // переживает удаление автора жалобы (как Reassignment-edits).
        builder.HasOne<Domain.Aggregates.User.User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // FK на админа-резолвера — тоже SetNull.
        builder.HasOne<Domain.Aggregates.User.User>()
            .WithMany()
            .HasForeignKey(x => x.ResolvedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // D25.2. Messages — каскадное удаление при удалении тикета.
        // Маппим публичное навигационное свойство Messages, EF Core
        // сам найдёт backing field _messages (по конвенции имени).
        builder.HasMany(x => x.Messages)
            .WithOne()
            .HasForeignKey(m => m.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Messages)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // D33. Attachments — каскадное удаление при удалении тикета.
        // Аналогично Messages — backing field _attachments.
        builder.HasMany(x => x.Attachments)
            .WithOne()
            .HasForeignKey(a => a.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Attachments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // "Мои обращения" юзера — выборка по user_id + сортировка по
        // CreatedAtUtc DESC.
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_support_tickets_user_id");

        // Админский фильтр по статусу — "покажи все Open + Urgent".
        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_support_tickets_status");

        builder.HasIndex(x => x.Severity)
            .HasDatabaseName("ix_support_tickets_severity");

        builder.HasIndex(x => x.Kind)
            .HasDatabaseName("ix_support_tickets_kind");

        // Сортировка списка — DESC по CreatedAtUtc.
        builder.HasIndex(x => x.CreatedAtUtc)
            .IsDescending()
            .HasDatabaseName("ix_support_tickets_created_at_utc");
    }
}
