using GdeOni.Domain.Aggregates.Deceased;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

/// <summary>
/// Конфигурация агрегата Deceased.
/// Здесь мы описываем:
/// - таблицу deceased
/// - primary key
/// - простые поля агрегата
/// - value objects через OwnsOne
/// - дочерние коллекции Photos и Memories
/// </summary>
public sealed class DeceasedConfiguration : IEntityTypeConfiguration<Deceased>
{
    public void Configure(EntityTypeBuilder<Deceased> builder)
    {
        // Имя таблицы в PostgreSQL
        builder.ToTable("deceaseds");

        // Первичный ключ
        builder.HasKey(x => x.Id);

        // Id генерируется в домене через Guid.NewGuid(),
        // поэтому EF не должен пытаться генерировать его сам
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Короткое описание умершего
        builder.Property(x => x.ShortDescription)
            .HasColumnName("short_description")
            .HasMaxLength(1000);

        // Полная биография
        builder.Property(x => x.Biography)
            .HasColumnName("biography");

        // Id пользователя, который создал карточку умершего
        builder.Property(x => x.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        // Дата создания записи
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        // Дата последнего обновления
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        // Подтверждена ли запись модератором / системой
        builder.Property(x => x.IsVerified)
            .HasColumnName("is_verified")
            .IsRequired();
        
        builder.Property(x => x.SearchKey)
            .HasColumnName("search_key")
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasIndex(x => x.SearchKey)
            .IsUnique()
            .HasDatabaseName(DbConstraints.DeceasedSearchKey);

        // -----------------------------
        // Value Object: PersonName
        // -----------------------------
        // PersonName не хранится в отдельной таблице.
        // Он "встраивается" в таблицу deceased.
        builder.OwnsOne(x => x.Name, name =>
        {
            name.Property(x => x.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(200)
                .IsRequired();

            name.Property(x => x.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(200)
                .IsRequired();

            name.Property(x => x.MiddleName)
                .HasColumnName("middle_name")
                .HasMaxLength(200);
        });

        // -----------------------------
        // Value Object: LifePeriod
        // -----------------------------
        // Также хранится прямо в таблице deceased.
        builder.OwnsOne(x => x.LifePeriod, lifePeriod =>
        {
            lifePeriod.Property(x => x.BirthDate)
                .HasColumnName("birth_date");

            lifePeriod.Property(x => x.DeathDate)
                .HasColumnName("death_date")
                .IsRequired();
        });

        // -----------------------------
        // Value Object: BurialLocation
        // -----------------------------
        // Поля места захоронения тоже лежат в deceased.
        builder.OwnsOne(x => x.BurialLocation, location =>
        {
            location.Property(x => x.Latitude)
                .HasColumnName("latitude")
                .IsRequired();

            location.Property(x => x.Longitude)
                .HasColumnName("longitude")
                .IsRequired();

            location.Property(x => x.Country)
                .HasColumnName("country")
                .HasMaxLength(200)
                .IsRequired();

            location.Property(x => x.Region)
                .HasColumnName("region")
                .HasMaxLength(200);

            location.Property(x => x.City)
                .HasColumnName("city")
                .HasMaxLength(200);

            location.Property(x => x.CemeteryName)
                .HasColumnName("cemetery_name")
                .HasMaxLength(300);

            location.Property(x => x.PlotNumber)
                .HasColumnName("plot_number")
                .HasMaxLength(100);

            location.Property(x => x.GraveNumber)
                .HasColumnName("grave_number")
                .HasMaxLength(100);

            // Enum в БД обычно удобно хранить как int
            location.Property(x => x.Accuracy)
                .HasColumnName("location_accuracy")
                .HasConversion<int>()
                .IsRequired();
        });
        
        builder.OwnsOne(x => x.Metadata, metadata =>
        {
            metadata.ToJson();

            metadata.Property(x => x.Epitaph)
                .HasMaxLength(500);

            metadata.Property(x => x.Religion)
                .HasMaxLength(200);

            metadata.Property(x => x.Source)
                .HasMaxLength(500);

            metadata.Property(x => x.AdditionalInfo)
                .HasMaxLength(2000);
        });

        // Явно говорим EF, что value objects обязательны.
        // Это особенно полезно для корректной материализации.
        builder.Navigation(x => x.Name).IsRequired();
        builder.Navigation(x => x.LifePeriod).IsRequired();
        builder.Navigation(x => x.BurialLocation).IsRequired();
        builder.Navigation(x => x.Metadata).IsRequired();
        
        // -----------------------------
        // Связь: кто создал запись
        // -----------------------------
        // Не делаем каскадное удаление.
        // Если удалить пользователя, карточки умерших удаляться не должны.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Photos
        builder.HasMany(x => x.Photos)
            .WithOne()
            .HasForeignKey("deceased_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Photos)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Memories
        builder.HasMany(x => x.Memories)
            .WithOne()
            .HasForeignKey("deceased_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Memories)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // -----------------------------
        // Индексы
        // -----------------------------
        // Индекс по создателю записи
        builder.HasIndex(x => x.CreatedByUserId)
            .HasDatabaseName("ix_deceased_created_by_user_id");

        // Индекс по флагу верификации
        builder.HasIndex(x => x.IsVerified)
            .HasDatabaseName("ix_deceased_is_verified");

        // Индекс по дате создания
        builder.HasIndex(x => x.CreatedAtUtc)
            .HasDatabaseName("ix_deceased_created_at_utc");
    }
}