using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

/// <summary>
/// D24. Маппинг audit-записей правок карточки умершего.
/// Таблица deceased_edits, jsonb для ChangesJson.
/// </summary>
public sealed class DeceasedEditConfiguration : IEntityTypeConfiguration<DeceasedEdit>
{
    public void Configure(EntityTypeBuilder<DeceasedEdit> builder)
    {
        builder.ToTable("deceased_edits");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.DeceasedId)
            .HasColumnName("deceased_id")
            .IsRequired();

        builder.Property(x => x.EditedByUserId)
            .HasColumnName("edited_by_user_id");

        builder.Property(x => x.EditedAtUtc)
            .HasColumnName("edited_at_utc")
            .IsRequired();

        builder.Property(x => x.Kind)
            .HasColumnName("kind")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ChangesJson)
            .HasColumnName("changes_json")
            .HasColumnType("jsonb")
            .HasMaxLength(DeceasedEdit.MaxChangesJsonLength)
            .IsRequired();

        // SET NULL — юзера могут soft-delete'ить, но история должна
        // переживать удаление автора правки (иначе админ-инвестигация
        // ломается).
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.EditedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Главный индекс админ-листинга: правки одной карточки от свежих
        // к старым. EditedAtUtc — desc, чтобы оптимизатор не сортировал.
        builder.HasIndex(x => new { x.DeceasedId, x.EditedAtUtc })
            .HasDatabaseName("ix_deceased_edits_deceased_id_edited_at_utc")
            .IsDescending(false, true);

        // Для разбора жалоб на конкретного юзера — "что он наделал".
        builder.HasIndex(x => x.EditedByUserId)
            .HasDatabaseName("ix_deceased_edits_edited_by_user_id");
    }
}
