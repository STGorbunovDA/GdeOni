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

        // F42. Наборы «за сколько дней» напоминать о годовщинах — CSV-строки
        // (например «0,7»). Пустая строка = напоминание выключено.
        builder.Property(x => x.DeathAnniversaryLeadDaysCsv)
            .HasColumnName("death_anniversary_lead_days")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.BirthAnniversaryLeadDaysCsv)
            .HasColumnName("birth_anniversary_lead_days")
            .HasMaxLength(64)
            .IsRequired();

        // Вычисляемые свойства (набор дней списком + булевы флаги обратной
        // совместимости) не мапятся в колонки — считаются из CSV в памяти.
        builder.Ignore(x => x.DeathAnniversaryLeadDays);
        builder.Ignore(x => x.BirthAnniversaryLeadDays);
        builder.Ignore(x => x.NotifyOnDeathAnniversary);
        builder.Ignore(x => x.NotifyOnBirthAnniversary);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.TrackedAtUtc)
            .HasColumnName("tracked_at_utc")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(x => x.DeceasedId)
            .HasDatabaseName("ix_tracked_deceased_deceased_id");

        builder.HasOne<Deceased>()
            .WithMany()
            .HasForeignKey(x => x.DeceasedId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composite index по (user_id, deceased_id) уникальный — он же
        // покрывает запросы по префиксу user_id (RemoveTracking,
        // GetMyTrackedDeceasedList). Отдельный ix_tracked_deceased_user_id
        // дублировал бы функциональность. См. D7.52.
        builder.HasIndex("user_id", nameof(TrackedDeceased.DeceasedId))
            .IsUnique()
            .HasDatabaseName("ux_tracked_deceased_user_id_deceased_id");
    }
}