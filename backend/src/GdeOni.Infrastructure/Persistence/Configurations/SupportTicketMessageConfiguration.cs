using GdeOni.Domain.Aggregates.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

public sealed class SupportTicketMessageConfiguration : IEntityTypeConfiguration<SupportTicketMessage>
{
    public void Configure(EntityTypeBuilder<SupportTicketMessage> builder)
    {
        builder.ToTable("support_ticket_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.TicketId)
            .HasColumnName("ticket_id")
            .IsRequired();

        builder.Property(x => x.AuthorKind)
            .HasColumnName("author_kind")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.AuthorUserId)
            .HasColumnName("author_user_id");

        builder.Property(x => x.Text)
            .HasColumnName("text")
            .HasMaxLength(SupportTicketMessage.MaxTextLength)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        // FK на автора: SetNull при удалении юзера. История остаётся,
        // конкретный автор анонимизируется.
        builder.HasOne<Domain.Aggregates.User.User>()
            .WithMany()
            .HasForeignKey(x => x.AuthorUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Чтение чата: WHERE ticket_id = X ORDER BY created_at ASC.
        builder.HasIndex(x => new { x.TicketId, x.CreatedAtUtc })
            .HasDatabaseName("ix_support_ticket_messages_ticket_id_created_at");
    }
}
