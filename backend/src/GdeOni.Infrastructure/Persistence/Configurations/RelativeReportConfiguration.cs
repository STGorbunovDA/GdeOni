using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.Relatives;
using GdeOni.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

internal sealed class RelativeReportConfiguration : IEntityTypeConfiguration<RelativeReport>
{
    public void Configure(EntityTypeBuilder<RelativeReport> builder)
    {
        builder.ToTable("relative_reports");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.ReporterUserId)
            .HasColumnName("reporter_user_id")
            .IsRequired();

        builder.Property(x => x.ReportedUserId)
            .HasColumnName("reported_user_id")
            .IsRequired();

        builder.Property(x => x.DeceasedId)
            .HasColumnName("deceased_id")
            .IsRequired();

        // Диалог-контекст — мягкая ссылка без FK: если диалог удалят, жалоба
        // остаётся (модерационная запись важнее ссылки на переписку).
        builder.Property(x => x.ConversationId)
            .HasColumnName("conversation_id");

        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasMaxLength(RelativeReport.MaxReasonLength)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        // Автор решения — audit-поле без FK (если админа удалят, id остаётся).
        builder.Property(x => x.ResolvedByUserId)
            .HasColumnName("resolved_by_user_id");

        builder.Property(x => x.ResolvedAtUtc)
            .HasColumnName("resolved_at_utc");

        builder.Property(x => x.ResolutionNote)
            .HasColumnName("resolution_note")
            .HasMaxLength(RelativeReport.MaxResolutionNoteLength);

        // Админский список — по статусу и дате.
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });

        // Дедуп pending-жалоб от одного репортера на одного и того же в диалоге
        // считаем в use case; поддерживающий индекс на выборку по репортеру.
        builder.HasIndex(x => new { x.ReporterUserId, x.ReportedUserId, x.ConversationId });

        // Cascade от репортера, нарушителя и карточки: удаляют сторону —
        // жалобы уходят вместе с ней (два FK на users, PostgreSQL допускает).
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ReporterUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ReportedUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Deceased>()
            .WithMany()
            .HasForeignKey(x => x.DeceasedId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
