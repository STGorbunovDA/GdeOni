using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Infrastructure.Persistence;

namespace GdeOni.Infrastructure.Tests.Persistence;

/// <summary>
/// Хелперы для seed-данных в репозиторных тестах. Domain-инварианты
/// заставляют создавать сначала User (FK с deceased_records.created_by_user_id
/// и tracked_deceased.user_id), потом Deceased — иначе вставка падает
/// с PostgresException 23503.
/// </summary>
internal static class TestData
{
    public static User SeedUser(AppDbContext dbContext, string? userNameSuffix = null)
    {
        var email = $"u-{Guid.NewGuid():N}@example.com";
        var userName = $"user-{userNameSuffix ?? Guid.NewGuid().ToString("N")}";
        var user = User.Register(email, "hash", userName: userName).Value;
        dbContext.Users.Add(user);
        return user;
    }

    public static Deceased SeedDeceased(
        AppDbContext dbContext,
        Guid createdByUserId,
        string firstName,
        string lastName,
        string? middleName = null,
        BurialLocation? burialLocation = null)
    {
        var deceased = Deceased.Create(
            firstName,
            lastName,
            middleName,
            birthDate: null,
            // Уникализируем deathDate — иначе SearchKey совпадут у тестов
            // с одинаковыми именами и Save() упадёт UniqueConstraintException.
            deathDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-Math.Abs(Guid.NewGuid().GetHashCode() % 5000))),
            burialLocation: burialLocation,
            createdByUserId: createdByUserId).Value;

        dbContext.DeceasedRecords.Add(deceased);
        return deceased;
    }
}
