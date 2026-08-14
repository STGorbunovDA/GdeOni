using GdeOni.Domain.Aggregates.User;
using GdeOni.Infrastructure.Notifications.Push;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

internal sealed class PushSubscriptionConfiguration
    : IEntityTypeConfiguration<PushSubscriptionRecord>
{
    public void Configure(EntityTypeBuilder<PushSubscriptionRecord> builder)
    {
        builder.ToTable("push_subscriptions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.Endpoint)
            .HasColumnName("endpoint")
            .HasMaxLength(PushSubscriptionRecord.MaxEndpointLength)
            .IsRequired();

        builder.Property(x => x.P256dh)
            .HasColumnName("p256dh")
            .HasMaxLength(PushSubscriptionRecord.MaxKeyLength)
            .IsRequired();

        builder.Property(x => x.Auth)
            .HasColumnName("auth")
            .HasMaxLength(PushSubscriptionRecord.MaxKeyLength)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(x => x.LastSuccessAtUtc)
            .HasColumnName("last_success_at_utc");

        // Endpoint — адрес конкретного устройства, дублей быть не должно:
        // иначе одно уведомление придёт на телефон несколько раз.
        builder.HasIndex(x => x.Endpoint)
            .IsUnique()
            .HasDatabaseName("ux_push_subscriptions_endpoint");

        // Рассылка идёт «все подписки пользователя».
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_push_subscriptions_user_id");

        // Удалили пользователя — его подписки уходят вместе с ним.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
