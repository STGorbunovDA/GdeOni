using GdeOni.Domain.Aggregates.Deceased;
using GdeOni.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;


/// <summary>
/// Конфигурация сущности DeceasedPhoto
/// Это дочерняя сущность агрегата Deceased
/// Она живет отдельно в своей таблице, но принадлежит умершему
/// </summary>
public sealed class DeceasedPhotoConfiguration : IEntityTypeConfiguration<DeceasedPhoto>
{
    public void Configure(EntityTypeBuilder<DeceasedPhoto> builder)
    {
        // Имя таблицы
        builder.ToTable("deceased_photos");

        // Первичный ключ
        builder.HasKey(x => x.Id);

        // Id задается доменом, не БД
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Shadow property для внешнего ключа.
        // В доменной модели нет свойства DeceasedId,
        // но в БД связь нужна.
        builder.Property<Guid>("deceased_id")
            .HasColumnName("deceased_id")
            .IsRequired();

        // URL фото
        builder.Property(x => x.Url)
            .HasColumnName("url")
            .HasMaxLength(2000)
            .IsRequired();

        // Описание фотоф
        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        // Является ли фото главным
        builder.Property(x => x.IsPrimary)
            .HasColumnName("is_primary")
            .IsRequired();

        // Когда было добавлено фото
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        // Кто добавил фото
        builder.Property(x => x.AddedByUserId)
            .HasColumnName("added_by_user_id")
            .IsRequired();

        // Статус модерации фото
        builder.Property(x => x.ModerationStatus)
            .HasColumnName("moderation_status")
            .HasConversion<int>()
            .IsRequired();
        
        // -----------------------------
        // Связь: кто добавил фото
        // -----------------------------
        // Restrict: удаление пользователя не должно автоматически удалять фото.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.AddedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Индекс для быстрого поиска всех фото умершего
        builder.HasIndex("deceased_id")
            .HasDatabaseName("ix_deceased_photos_deceased_id");

        // Индекс по пользователю, который загрузил фото
        builder.HasIndex(x => x.AddedByUserId)
            .HasDatabaseName("ix_deceased_photos_added_by_user_id");
    }
}