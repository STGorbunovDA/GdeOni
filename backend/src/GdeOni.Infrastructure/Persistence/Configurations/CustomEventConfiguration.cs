using GdeOni.Domain.Aggregates.Events;
using GdeOni.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

public sealed class CustomEventConfiguration : IEntityTypeConfiguration<CustomEvent>
{
    public void Configure(EntityTypeBuilder<CustomEvent> builder)
    {
        builder.ToTable("custom_events");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(CustomEvent.MaxTitleLength)
            .IsRequired();

        builder.Property(x => x.EventDate)
            .HasColumnName("event_date")
            .IsRequired();

        // Набор «за сколько дней» в CSV — как у holiday_reminders.
        builder.Property(x => x.LeadDaysCsv)
            .HasColumnName("lead_days")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        // Быстрая выборка событий пользователя.
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_custom_events_user_id");

        // Удаление пользователя — каскадом.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
