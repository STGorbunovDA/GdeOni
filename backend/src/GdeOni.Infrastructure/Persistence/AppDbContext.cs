using GdeOni.Domain.Aggregates.Auth;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.Subscriptions;
using GdeOni.Domain.Aggregates.User;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}