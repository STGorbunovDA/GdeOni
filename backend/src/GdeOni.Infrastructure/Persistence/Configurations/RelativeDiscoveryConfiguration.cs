using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using GdeOni.Infrastructure.Relatives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

internal sealed class RelativeDiscoveryConfiguration : IEntityTypeConfiguration<RelativeDiscovery>
{
    public void Configure(EntityTypeBuilder<RelativeDiscovery> builder)
    {
        builder.ToTable("relative_discoveries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        builder.Property(x => x.DeceasedId)
            .HasColumnName("deceased_id")
            .IsRequired();

        builder.Property(x => x.RelativeUserId)
            .HasColumnName("relative_user_id")
            .IsRequired();

        builder.Property(x => x.DiscoveredAtUtc)
            .HasColumnName("discovered_at_utc")
            .IsRequired();

        builder.Property(x => x.IsNew)
            .HasColumnName("is_new")
            .IsRequired();

        // Дедуп: одна запись на (владелец, умерший, родственник).
        builder.HasIndex(x => new { x.OwnerUserId, x.DeceasedId, x.RelativeUserId })
            .IsUnique()
            .HasDatabaseName(DbConstraints.UxRelativeDiscoveries);

        // Быстрый фильтр «новые для владельца» (частичный по is_new не делаем —
        // объём маленький, обычного составного индекса хватает).
        builder.HasIndex(x => new { x.OwnerUserId, x.IsNew });

        // Cascade от владельца, родственника и карточки: если удалят любую из
        // сторон — лог-строки уходят вместе с ней. PostgreSQL допускает
        // несколько cascade-путей к одной таблице (два FK на users).
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.RelativeUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Deceased>()
            .WithMany()
            .HasForeignKey(x => x.DeceasedId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
