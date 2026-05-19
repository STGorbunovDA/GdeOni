using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(User.MaxEmailLength)
            .IsRequired();

        builder.Property(x => x.UserName)
            .HasColumnName("user_name")
            .HasMaxLength(User.MaxUserNameLength)
            .IsRequired();

        builder.Property(x => x.UserNameNormalized)
            .HasColumnName("user_name_normalized")
            .HasMaxLength(User.MaxUserNameLength)
            .IsRequired();

        builder.Property(x => x.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(User.MaxFullNameLength);

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(User.MaxPasswordHash)
            .IsRequired();

        builder.Property(x => x.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(User.MaxRole)
            .IsRequired();

        builder.Property(x => x.RegisteredAtUtc)
            .HasColumnName("registered_at_utc")
            .IsRequired();

        builder.Property(x => x.LastLoginAtUtc)
            .HasColumnName("last_login_at_utc");

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(x => x.SecurityStamp)
            .HasColumnName("security_stamp")
            .IsRequired();

        // D19. Legal acceptance: четыре колонки, заполняются единым
        // методом User.AcceptLegal в одной транзакции — допускаем
        // NULL timestamps для исторических юзеров до миграции D19.
        builder.Property(x => x.PrivacyPolicyAcceptedAtUtc)
            .HasColumnName("privacy_policy_accepted_at_utc");

        builder.Property(x => x.TermsAcceptedAtUtc)
            .HasColumnName("terms_accepted_at_utc");

        builder.Property(x => x.PrivacyPolicyVersion)
            .HasColumnName("privacy_policy_version")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.TermsVersion)
            .HasColumnName("terms_version")
            .HasDefaultValue(0)
            .IsRequired();

        builder.OwnsOne(x => x.Subscription, subscription =>
        {
            subscription.Property(x => x.Status)
                .HasColumnName("subscription_status")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            subscription.Property(x => x.Plan)
                .HasColumnName("subscription_plan")
                .HasConversion<string?>()
                .HasMaxLength(50);

            subscription.Property(x => x.CurrentPeriodStartedAtUtc)
                .HasColumnName("subscription_current_period_started_at_utc");

            subscription.Property(x => x.ExpiresAtUtc)
                .HasColumnName("subscription_expires_at_utc");

            subscription.Property(x => x.LastPaymentId)
                .HasColumnName("subscription_last_payment_id")
                .HasMaxLength(Domain.Aggregates.User.Subscription.MaxPaymentIdLength);

            subscription.Property(x => x.CancelledAtUtc)
                .HasColumnName("subscription_cancelled_at_utc");

            // D16. Индекс по ExpiresAtUtc нужен для будущей фоновой job'ы
            // "expired marking" и для аналитических запросов типа "сколько
            // юзеров истечёт за ближайшие 7 дней". Для гейта (handler
            // фильтрует уже загруженного юзера) индекс не критичен.
            subscription.HasIndex(x => x.ExpiresAtUtc)
                .HasDatabaseName("ix_users_subscription_expires_at_utc");

            // D16. Поиск юзера по externalPaymentId для
            // ProcessPaymentWebhook — без индекса full scan на каждом
            // webhook'е. Не уникальный: возможен retry с тем же payment id.
            subscription.HasIndex(x => x.LastPaymentId)
                .HasDatabaseName("ix_users_subscription_last_payment_id");
        });

        builder.Navigation(x => x.Subscription).IsRequired();

        builder.HasMany(x => x.TrackedDeceasedItems)
            .WithOne()
            .HasForeignKey("user_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.TrackedDeceasedItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName(DbConstraints.UxUsersEmail);

        builder.HasIndex(x => x.UserNameNormalized)
            .IsUnique()
            .HasDatabaseName(DbConstraints.UxUsersName);

        // GetAllUsersUseCase фильтрует WHERE role = ... и сортирует
        // ORDER BY registered_at_utc DESC; без индексов sequential scan
        // на масштабе пользовательской базы (см. D11.5.1).
        builder.HasIndex(x => x.Role)
            .HasDatabaseName("ix_users_role");

        builder.HasIndex(x => x.RegisteredAtUtc)
            .IsDescending()
            .HasDatabaseName("ix_users_registered_at_utc");
    }
}