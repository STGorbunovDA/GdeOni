using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using GdeOni.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

internal sealed class SentAnniversaryEmailConfiguration : IEntityTypeConfiguration<SentAnniversaryEmail>
{
    public void Configure(EntityTypeBuilder<SentAnniversaryEmail> builder)
    {
        builder.ToTable("sent_anniversary_emails");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.DeceasedId)
            .HasColumnName("deceased_id")
            .IsRequired();

        builder.Property(x => x.Kind)
            .HasColumnName("kind")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.AnniversaryDate)
            .HasColumnName("anniversary_date")
            .IsRequired();

        builder.Property(x => x.SentAtUtc)
            .HasColumnName("sent_at_utc")
            .IsRequired();

        // Дедуп: одно письмо на (пользователь, умерший, тип, дата годовщины).
        builder.HasIndex(x => new { x.UserId, x.DeceasedId, x.Kind, x.AnniversaryDate })
            .IsUnique()
            .HasDatabaseName(DbConstraints.UxSentAnniversaryEmails);

        // Cascade от пользователя и карточки: если удалят одну из сторон,
        // дедуп-строки уходят вместе с ней (иначе FK блокирует delete).
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Deceased>()
            .WithMany()
            .HasForeignKey(x => x.DeceasedId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
