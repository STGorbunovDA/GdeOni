using GdeOni.Domain.Aggregates.Deceased;
using GdeOni.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

/// <summary>
/// Конфигурация сущности DeceasedMemoryEntry.
/// Это воспоминания памяти об умершем.
/// Они принадлежат агрегату Deceased и лежат в отдельной таблице.
/// </summary>
public sealed class DeceasedMemoryEntryConfiguration : IEntityTypeConfiguration<DeceasedMemoryEntry>
{
    public void Configure(EntityTypeBuilder<DeceasedMemoryEntry> builder)
    {
        // Имя таблицы
        builder.ToTable("deceased_memory_entries");

        // Первичный ключ
        builder.HasKey(x => x.Id);

        // Id генерируется доменом
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Shadow FK на умершего
        builder.Property<Guid>("deceased_id")
            .HasColumnName("deceased_id")
            .IsRequired();

        // Текст воспоминания
        builder.Property(x => x.Text)
            .HasColumnName("text")
            .IsRequired();

        // Отображаемое имя автора
        builder.Property(x => x.AuthorDisplayName)
            .HasColumnName("author_display_name")
            .HasMaxLength(300)
            .IsRequired();

        // Id пользователя-автора, если сообщение не анонимное
        builder.Property(x => x.AuthorUserId)
            .HasColumnName("author_user_id");

        // Дата создания
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        // Статус модерации
        builder.Property(x => x.ModerationStatus)
            .HasColumnName("moderation_status")
            .HasConversion<int>()
            .IsRequired();
        
        // -----------------------------
        // Связь: пользователь-автор воспоминания
        // -----------------------------
        // SetNull: если пользователя удалили,
        // воспоминание сохраняется, но ссылка на автора обнуляется.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.AuthorUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Индекс по умершему
        builder.HasIndex("deceased_id")
            .HasDatabaseName("ix_memory_entries_deceased_id");

        // Индекс по автору
        builder.HasIndex(x => x.AuthorUserId)
            .HasDatabaseName("ix_memory_entries_author_user_id");
    }
}