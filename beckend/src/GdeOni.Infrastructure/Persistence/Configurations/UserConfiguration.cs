using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

/// <summary>
/// Конфигурация агрегата User.
/// Здесь описывается:
/// - таблица users
/// - простые поля пользователя
/// - приватная коллекция отслеживаемых умерших
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Имя таблицы
        builder.ToTable("users");

        // Первичный ключ
        builder.HasKey(x => x.Id);

        // Id приходит из домена
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Email пользователя
        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        // Ник/имя пользователя
        builder.Property(x => x.UserName)
            .HasColumnName("user_name")
            .HasMaxLength(100)
            .IsRequired();

        // Полное имя
        builder.Property(x => x.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(300);

        // Хэш пароля
        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(1000)
            .IsRequired();

        // Дата регистрации
        builder.Property(x => x.RegisteredAtUtc)
            .HasColumnName("registered_at_utc")
            .IsRequired();

        // Дата последнего входа
        builder.Property(x => x.LastLoginAtUtc)
            .HasColumnName("last_login_at_utc");

        builder.HasMany(x => x.TrackedDeceasedItems)
            .WithOne()
            .HasForeignKey("user_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.TrackedDeceasedItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // -----------------------------
        // Уникальные индексы
        // -----------------------------
        // Email должен быть уникален
        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName(DbConstraints.UxUsersEmail);

        builder.HasIndex(x => x.UserName)
            .IsUnique()
            .HasDatabaseName(DbConstraints.UxUsersName);
    }
}