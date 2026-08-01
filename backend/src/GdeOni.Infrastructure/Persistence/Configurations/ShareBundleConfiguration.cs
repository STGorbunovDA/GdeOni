using GdeOni.Domain.Aggregates.Sharing;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

public sealed class ShareBundleConfiguration : IEntityTypeConfiguration<ShareBundle>
{
    public void Configure(EntityTypeBuilder<ShareBundle> builder)
    {
        builder.ToTable("share_bundles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(ShareBundle.MaxCodeLength)
            .IsRequired();

        builder.Property(x => x.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .IsRequired();

        // Список id карточек — нативный postgres uuid[]. Отдельная таблица
        // не нужна: запросов «по id внутри подборки» нет, только «по коду».
        builder.Property(x => x.DeceasedIds)
            .HasColumnName("deceased_ids")
            .HasColumnType("uuid[]")
            .IsRequired();

        // Поиск подборки по коду — единственный горячий запрос. Уникальный.
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName(DbConstraints.UxShareBundlesCode);

        // Для будущей фоновой чистки протухших подборок.
        builder.HasIndex(x => x.ExpiresAtUtc)
            .HasDatabaseName("ix_share_bundles_expires_at_utc");

        // Автор подборки. SetNull — как у прочих audit-ссылок на юзера
        // (blocked_by и т.п.): юзера можно удалить, подборка остаётся
        // анонимной до истечения срока.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
