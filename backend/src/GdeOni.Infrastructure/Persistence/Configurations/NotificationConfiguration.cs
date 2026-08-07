using GdeOni.Domain.Aggregates.Notifications;
using GdeOni.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.RecipientUserId)
            .HasColumnName("recipient_user_id")
            .IsRequired();

        builder.Property(x => x.Kind)
            .HasColumnName("kind")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(Notification.MaxTitleLength)
            .IsRequired();

        builder.Property(x => x.Body)
            .HasColumnName("body")
            .HasMaxLength(Notification.MaxBodyLength);

        builder.Property(x => x.Link)
            .HasColumnName("link")
            .HasMaxLength(Notification.MaxLinkLength);

        builder.Property(x => x.IsRead)
            .HasColumnName("is_read")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(x => x.ReadAtUtc)
            .HasColumnName("read_at_utc");

        // Выборка «мои последние» + счётчик непрочитанных — по получателю,
        // флагу прочтения и дате.
        builder.HasIndex(x => new { x.RecipientUserId, x.IsRead, x.CreatedAtUtc });

        // Cascade от получателя: удалили пользователя — его уведомления уходят
        // вместе с ним.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
