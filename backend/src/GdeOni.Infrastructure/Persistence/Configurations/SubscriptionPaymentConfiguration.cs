using GdeOni.Domain.Aggregates.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPaymentConfiguration : IEntityTypeConfiguration<SubscriptionPayment>
{
    public void Configure(EntityTypeBuilder<SubscriptionPayment> builder)
    {
        builder.ToTable("subscription_payments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.ExternalPaymentId)
            .HasColumnName("external_payment_id")
            .HasMaxLength(SubscriptionPayment.MaxExternalIdLength)
            .IsRequired();

        builder.Property(x => x.Plan)
            .HasColumnName("plan")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.AmountRub)
            .HasColumnName("amount_rub")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CheckoutUrl)
            .HasColumnName("checkout_url")
            .HasMaxLength(SubscriptionPayment.MaxCheckoutUrlLength);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(x => x.PeriodStartUtc)
            .HasColumnName("period_start_utc");

        builder.Property(x => x.PeriodEndUtc)
            .HasColumnName("period_end_utc");

        // D23. Webhook lookup — должно быть точечно по externalPaymentId,
        // не full scan. Unique — YooKassa никогда не возвращает один и
        // тот же id для разных платежей.
        builder.HasIndex(x => x.ExternalPaymentId)
            .IsUnique()
            .HasDatabaseName("ux_subscription_payments_external_payment_id");

        // D23. Админ-выборка "платежи юзера" + "список юзера по своим
        // платежам".
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_subscription_payments_user_id");

        // D23. Админ-фильтр по статусу (типа "покажи все Failed за
        // вчера").
        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_subscription_payments_status");

        // D23. Дефолтная сортировка админ-таблицы — DESC по CreatedAt.
        builder.HasIndex(x => x.CreatedAtUtc)
            .IsDescending()
            .HasDatabaseName("ix_subscription_payments_created_at_utc");
    }
}
