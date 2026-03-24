using GdeOni.Domain.Aggregates.Deceased;
using GdeOni.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;

namespace GdeOni.Infrastructure.Persistence;

/// <summary>
/// Главный DbContext приложения.
/// Он знает только о корневых агрегатах:
/// - Deceased
/// - User
/// Вложенные сущности (DeceasedPhoto, MemoryEntry, TrackedDeceased)
/// будут подтянуты через конфигурации.
/// </summary>
public sealed class AppDbContext : DbContext
{
    // Корневой агрегат умерших
    public DbSet<Deceased> DeceasedRecords => Set<Deceased>();

    // Корневой агрегат пользователей
    public DbSet<User> Users => Set<User>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}