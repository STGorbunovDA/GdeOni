using GdeOni.Application.Common.Security;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using GdeOni.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GdeOni.Infrastructure.Data;

internal static class DbInitializer
{
    /// <summary>
    /// Fail-fast: проверяет что в БД накатаны все миграции из сборки.
    /// Если есть pending — кидаем исключение на старте, иначе API
    /// бы стартовал и падал на первом же запросе с непонятной ошибкой
    /// EF Core ("column X doesn't exist"). Auto-migrate намеренно
    /// не делаем — это рискованно без бэкапа на проде.
    /// </summary>
    internal static async Task EnsureMigrationsAppliedAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var logger = sp.GetRequiredService<ILogger<AppDbContext>>();
        var dbContext = sp.GetRequiredService<AppDbContext>();

        var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken))
            .ToList();
        if (pending.Count == 0)
        {
            logger.LogInformation("Все EF миграции накатаны.");
            return;
        }

        var pendingList = string.Join(", ", pending);
        logger.LogCritical(
            "В БД есть pending EF миграции ({Count}): {Pending}. " +
            "Накати их через `dotnet ef database update` и перезапусти.",
            pending.Count, pendingList);
        throw new InvalidOperationException(
            $"Pending EF migrations: {pendingList}. " +
            "Apply them before starting the API.");
    }

    internal static async Task SeedSuperAdminAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var logger = sp.GetRequiredService<ILogger<AppDbContext>>();
        var options = sp.GetRequiredService<IOptions<SeedOptions>>().Value;

        var superAdmin = options.SuperAdmin;
        if (superAdmin is null ||
            string.IsNullOrWhiteSpace(superAdmin.Email) ||
            string.IsNullOrWhiteSpace(superAdmin.Password))
        {
            logger.LogWarning(
                "Seed:SuperAdmin не сконфигурирован — пропускаю создание супер-админа.");
            return;
        }

        var dbContext = sp.GetRequiredService<AppDbContext>();
        var passwordHasher = sp.GetRequiredService<IPasswordHasher>();

        var anySuperAdmin = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Role == UserRole.SuperAdmin, cancellationToken);

        if (anySuperAdmin)
        {
            logger.LogInformation(
                "SuperAdmin уже существует — повторное создание не требуется.");
            await EnsureSuperAdminLoginIsEmailAsync(
                dbContext, logger, superAdmin.Email, cancellationToken);
            return;
        }

        var passwordHash = passwordHasher.Hash(superAdmin.Password);

        var userResult = User.RegisterSuperAdmin(
            email: superAdmin.Email,
            passwordHash: passwordHash,
            fullName: superAdmin.FullName,
            userName: superAdmin.UserName);

        if (userResult.IsFailure)
        {
            logger.LogError(
                "Не удалось создать SuperAdmin: {ErrorCode} {ErrorMessage}",
                userResult.Error.Code,
                userResult.Error.Message);
            return;
        }

        await dbContext.Users.AddAsync(userResult.Value, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Создан SuperAdmin с email {Email} (id {UserId}).",
            userResult.Value.Email,
            userResult.Value.Id);
    }

    /// <summary>
    /// Логин владельца сервиса всегда равен его email. Нужно для аккаунтов,
    /// заведённых до появления логина: миграция AddUserLogin проставила им
    /// префикс («admin»), а вход у супер-админа должен быть строго по адресу.
    ///
    /// Идемпотентно: если логин уже совпадает — ничего не делаем. Если
    /// нужный логин занят кем-то другим, оставляем как есть и пишем
    /// предупреждение — молча ломать чужую учётку нельзя.
    /// </summary>
    private static async Task EnsureSuperAdminLoginIsEmailAsync(
        AppDbContext dbContext,
        ILogger logger,
        string configuredEmail,
        CancellationToken cancellationToken)
    {
        var email = configuredEmail.Trim().ToLowerInvariant();

        var user = await dbContext.Users
            .FirstOrDefaultAsync(
                u => u.Role == UserRole.SuperAdmin && u.Email == email,
                cancellationToken);

        if (user is null || user.Login == email)
            return;

        var takenByOther = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Login == email && u.Id != user.Id, cancellationToken);

        if (takenByOther)
        {
            logger.LogWarning(
                "Логин {Login} занят другим пользователем — логин SuperAdmin не изменён.",
                email);
            return;
        }

        var result = user.ChangeLogin(email);
        if (result.IsFailure)
        {
            logger.LogWarning(
                "Не удалось привести логин SuperAdmin к email: {ErrorCode}.",
                result.Error.Code);
            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Логин SuperAdmin приведён к email {Email}.", email);
    }
}
