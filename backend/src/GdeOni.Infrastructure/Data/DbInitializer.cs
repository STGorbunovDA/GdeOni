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
            return;
        }

        var passwordHash = passwordHasher.Hash(superAdmin.Password);

        var userResult = User.Register(
            email: superAdmin.Email,
            passwordHash: passwordHash,
            fullName: superAdmin.FullName,
            userName: superAdmin.UserName,
            role: UserRole.SuperAdmin);

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
}
