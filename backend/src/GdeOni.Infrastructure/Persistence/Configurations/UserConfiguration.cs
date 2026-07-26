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

        // D19. Дата рождения — nullable для обратной совместимости с
        // юзерами, зарегистрированными до введения возрастного гарда.
        // Postgres date подставляется автоматически через
        // UseSnakeCaseNamingConvention.
        builder.Property(x => x.BirthDate)
            .HasColumnName("birth_date");

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

        // D43. Восстановление пароля по ссылке из письма. В колонке лежит
        // SHA-256 hex (64 символа), а не сам токен — как и у refresh-токенов.
        builder.Property(x => x.PasswordResetTokenHash)
            .HasColumnName("password_reset_token_hash")
            .HasMaxLength(128);

        builder.Property(x => x.PasswordResetTokenExpiresAtUtc)
            .HasColumnName("password_reset_token_expires_at_utc");

        // Поиск юзера по хешу токена — единственный запрос на этапе
        // подтверждения сброса. Фильтрованный индекс: активный токен есть
        // у единиц пользователей, индексировать NULL'ы смысла нет.
        builder.HasIndex(x => x.PasswordResetTokenHash)
            .HasDatabaseName("ix_users_password_reset_token_hash")
            .HasFilter("password_reset_token_hash IS NOT NULL");

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

        // D22. Complimentary access (бесплатный доступ от админа).
        // Четыре nullable-колонки, заполняются и обнуляются единым
        // методом User.Grant/RevokeComplimentaryAccess в одной
        // транзакции — нет смысла делать owned VO ради 4 примитивов.
        builder.Property(x => x.ComplimentaryAccessGrantedAtUtc)
            .HasColumnName("complimentary_access_granted_at_utc");

        builder.Property(x => x.ComplimentaryAccessUntilUtc)
            .HasColumnName("complimentary_access_until_utc");

        builder.Property(x => x.ComplimentaryAccessGrantedByAdminId)
            .HasColumnName("complimentary_access_granted_by_admin_id");

        builder.Property(x => x.ComplimentaryAccessNote)
            .HasColumnName("complimentary_access_note")
            .HasMaxLength(User.MaxComplimentaryNoteLength);

        // F17.10. Блокировка пользователя — четыре nullable-колонки
        // заполняются и обнуляются единым методом User.Block/Unblock.
        // IsBlocked имеет default=false для существующих юзеров после
        // миграции. BlockedReason — опциональный комментарий админа.
        builder.Property(x => x.IsBlocked)
            .HasColumnName("is_blocked")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.BlockedAtUtc)
            .HasColumnName("blocked_at_utc");

        builder.Property(x => x.BlockedByUserId)
            .HasColumnName("blocked_by_user_id");

        builder.Property(x => x.BlockedReason)
            .HasColumnName("blocked_reason")
            .HasMaxLength(User.MaxBlockReasonLength);

        // ix_users_is_blocked — для аналитических запросов "сколько
        // заблокированных" и потенциального батч-разблока. Партиал по
        // is_blocked = true минимизирует размер: подавляющее большинство
        // юзеров не заблокированы.
        builder.HasIndex(x => x.IsBlocked)
            .HasFilter("is_blocked = true")
            .HasDatabaseName("ix_users_is_blocked");

        // Self-reference FK: blocked_by_user_id и complimentary_access_granted_by_admin_id
        // указывают на админа, выполнившего действие. OnDelete=SetNull —
        // зеркало DeceasedEdit.EditedByUserId (D24): админа можно удалить,
        // запись о блокировке/complimentary остаётся, но "кем" — анонимно.
        // EF сам создаст индексы ix_users_blocked_by_user_id и
        // ix_users_complimentary_access_granted_by_admin_id (FK без индекса
        // делает full scan на удалении строки в users).
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.BlockedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ComplimentaryAccessGrantedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

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

        // UserName НЕ уникален (тёзки допустимы, вход по email) — уникальный
        // индекс ux_users_user_name снят миграцией RemoveUserNameUniqueIndex
        // (2026-07-26). Колонка user_name_normalized оставлена как есть.

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