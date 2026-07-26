using GdeOni.Domain.Aggregates.Events;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

public sealed class HolidayReminderConfiguration : IEntityTypeConfiguration<HolidayReminder>
{
    public void Configure(EntityTypeBuilder<HolidayReminder> builder)
    {
        builder.ToTable("holiday_reminders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        // Ключ праздника = его название (стабильно из года в год).
        builder.Property(x => x.HolidayKey)
            .HasColumnName("holiday_key")
            .HasMaxLength(HolidayReminder.MaxHolidayKeyLength)
            .IsRequired();

        // Набор «за сколько дней» в CSV («0,1,3,7») — простая строка, без
        // Postgres-массивов: провайдеро-независимо и без edge-кейсов маппинга.
        builder.Property(x => x.LeadDaysCsv)
            .HasColumnName("lead_days")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        // Одна настройка на пару (пользователь, праздник). Удаление юзера —
        // каскадом (настройки напоминаний не имеют смысла без него).
        builder.HasIndex(x => new { x.UserId, x.HolidayKey })
            .IsUnique()
            .HasDatabaseName(DbConstraints.UxHolidayRemindersUserKey);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
