using GdeOni.Domain.Aggregates.Deceased;
using GdeOni.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

/// <summary>
/// Конфигурация сущности TrackedDeceased.
/// Это дочерняя сущность агрегата User.
/// Она хранит факт, что пользователь отслеживает конкретного умершего,
/// а также персональные настройки:
/// - тип связи
/// - заметки
/// - уведомления
/// - статус отслеживания
/// </summary>
public sealed class TrackedDeceasedConfiguration : IEntityTypeConfiguration<TrackedDeceased>
{
    public void Configure(EntityTypeBuilder<TrackedDeceased> builder)
    {
        // Имя таблицы
        builder.ToTable("tracked_deceased");

        // Первичный ключ
        builder.HasKey(x => x.Id);

        // Id задается в домене
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Shadow FK на пользователя.
        // В доменной модели UserId нет как публичного свойства,
        // но в БД связь нужна.
        builder.Property<Guid>("user_id")
            .HasColumnName("user_id")
            .IsRequired();

        // Id умершего, которого отслеживают
        builder.Property(x => x.DeceasedId)
            .HasColumnName("deceased_id")
            .IsRequired();

        // Тип связи пользователя с умершим
        builder.Property(x => x.RelationshipType)
            .HasColumnName("relationship_type")
            .HasConversion<int>()
            .IsRequired();

        // Персональные заметки пользователя
        builder.Property(x => x.PersonalNotes)
            .HasColumnName("personal_notes")
            .HasMaxLength(2000);

        // Уведомлять ли о годовщине смерти
        builder.Property(x => x.NotifyOnDeathAnniversary)
            .HasColumnName("notify_on_death_anniversary")
            .IsRequired();

        // Уведомлять ли о дне рождения/године
        builder.Property(x => x.NotifyOnBirthAnniversary)
            .HasColumnName("notify_on_birth_anniversary")
            .IsRequired();

        // Статус отслеживания
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        // Когда пользователь начал отслеживать
        builder.Property(x => x.TrackedAtUtc)
            .HasColumnName("tracked_at_utc")
            .IsRequired();

        // Индекс по пользователю
        builder.HasIndex("user_id")
            .HasDatabaseName("ix_tracked_deceased_user_id");

        // Индекс по умершему
        builder.HasIndex(x => x.DeceasedId)
            .HasDatabaseName("ix_tracked_deceased_deceased_id");
        
        // -----------------------------
        // Связь: tracking -> deceased
        // -----------------------------
        // Cascade: если карточка умершего удалена,
        // отслеживание этой карточки тоже должно исчезнуть.
        builder.HasOne<Deceased>()
            .WithMany()
            .HasForeignKey(x => x.DeceasedId)
            .OnDelete(DeleteBehavior.Cascade);

        // Уникальный индекс:
        // один пользователь не должен иметь две записи отслеживания
        // для одного и того же умершего.
        // Важно:
        // если ты используешь архивацию и хочешь разрешить повторное
        // создание после архивации, тогда этот unique-индекс нужно
        // пересмотреть, либо перейти на "реактивацию" архивной записи.
        builder.HasIndex("user_id", nameof(TrackedDeceased.DeceasedId))
            .IsUnique()
            .HasDatabaseName("ux_tracked_deceased_user_id_deceased_id");
    }
}