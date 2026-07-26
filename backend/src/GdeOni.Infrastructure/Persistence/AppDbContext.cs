using GdeOni.Domain.Aggregates.Auth;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.Events;
using GdeOni.Domain.Aggregates.Subscriptions;
using GdeOni.Domain.Aggregates.Support;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;

namespace GdeOni.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Корневой агрегат умерших
    public DbSet<Deceased> DeceasedRecords => Set<Deceased>();

    // Корневой агрегат пользователей
    public DbSet<User> Users => Set<User>();

    // Refresh-токены (отдельный агрегат)
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // D23. История платежей.
    public DbSet<SubscriptionPayment> SubscriptionPayments => Set<SubscriptionPayment>();

    // D25. Универсальные тикеты службы поддержки (manual + auto).
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

    // Персональные настройки напоминаний о праздниках (per-user).
    public DbSet<HolidayReminder> HolidayReminders => Set<HolidayReminder>();

    // D37. Лог разосланных писем о годовщинах (дедупликация). Инфра-
    // сущность, не доменный агрегат.
    internal DbSet<SentAnniversaryEmail> SentAnniversaryEmails => Set<SentAnniversaryEmail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}