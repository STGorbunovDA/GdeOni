using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.Relatives;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

/// <summary>
/// Функция «Родственники»: диалог + сообщения. Один диалог на пару участников
/// в контексте карточки (уникальный индекс). Каскадное удаление: при удалении
/// карточки или любого из участников диалог (и его сообщения) уходят.
/// PostgreSQL допускает несколько cascade-путей к users, в отличие от SQL Server.
/// </summary>
public sealed class RelativeConversationConfiguration : IEntityTypeConfiguration<RelativeConversation>
{
    public void Configure(EntityTypeBuilder<RelativeConversation> builder)
    {
        builder.ToTable("relative_conversations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.DeceasedId).HasColumnName("deceased_id").IsRequired();
        builder.Property(x => x.ParticipantAId).HasColumnName("participant_a_id").IsRequired();
        builder.Property(x => x.ParticipantBId).HasColumnName("participant_b_id").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.LastMessageAtUtc).HasColumnName("last_message_at_utc").IsRequired();

        // Один диалог на пару (canonical A<B) в контексте карточки.
        builder.HasIndex(x => new { x.DeceasedId, x.ParticipantAId, x.ParticipantBId })
            .IsUnique()
            .HasDatabaseName(DbConstraints.UxRelativeConversationsPair);

        builder.HasOne<Deceased>()
            .WithMany()
            .HasForeignKey(x => x.DeceasedId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Domain.Aggregates.User.User>()
            .WithMany()
            .HasForeignKey(x => x.ParticipantAId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Domain.Aggregates.User.User>()
            .WithMany()
            .HasForeignKey(x => x.ParticipantBId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Messages)
            .WithOne()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Messages)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Свежие диалоги сверху.
        builder.HasIndex(x => x.LastMessageAtUtc)
            .IsDescending()
            .HasDatabaseName("ix_relative_conversations_last_message_at_utc");
    }
}

/// <summary>Сообщения переписки «Родственники».</summary>
public sealed class RelativeMessageConfiguration : IEntityTypeConfiguration<RelativeMessage>
{
    public void Configure(EntityTypeBuilder<RelativeMessage> builder)
    {
        builder.ToTable("relative_messages");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.ConversationId).HasColumnName("conversation_id").IsRequired();
        builder.Property(x => x.SenderId).HasColumnName("sender_id").IsRequired();

        builder.Property(x => x.Text)
            .HasColumnName("text")
            .HasMaxLength(RelativeMessage.MaxTextLength)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.EditedAtUtc).HasColumnName("edited_at_utc");

        builder.Property(x => x.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();
        builder.Property(x => x.DeletedAtUtc).HasColumnName("deleted_at_utc");

        builder.Property(x => x.IsRead)
            .HasColumnName("is_read")
            .HasDefaultValue(false)
            .IsRequired();
        builder.Property(x => x.ReadAtUtc).HasColumnName("read_at_utc");

        // Загрузка сообщений диалога по порядку + подсчёт непрочитанных.
        builder.HasIndex(x => new { x.ConversationId, x.CreatedAtUtc })
            .HasDatabaseName("ix_relative_messages_conversation_created");
    }
}
