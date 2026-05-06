using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

// Namespace отличается от корневого `GdeOni.Domain.Tests.Aggregates.User`,
// чтобы не было конфликта с `GdeOni.Domain.Aggregates.User.User`
// (компилятор начинает резолвить `User` из локального namespace).
namespace GdeOni.Domain.Tests.UserAggregate;

/// <summary>
/// Тесты агрегата <see cref="User"/> — корневая сущность аутентификации
/// и tracking-коллекции. Покрываем главный сценарий приложения:
/// добавление умершего в список отслеживаемых (TrackDeceased) и
/// обработку повторного добавления (Reactivate-путь).
/// </summary>
public sealed class UserTests
{
    private const string SampleEmail = "ivan@example.com";
    private const string SamplePasswordHash = "hash$with$enough$chars";

    /// <summary>
    /// TrackDeceased — главное действие пользователя в приложении.
    /// Ожидаем: tracking создаётся со статусом Active, попадает в
    /// IReadOnlyCollection TrackedDeceasedItems, передающиеся параметры
    /// (RelationshipType, PersonalNotes, флаги уведомлений) сохраняются.
    /// </summary>
    [Fact]
    public void TrackDeceased_NewDeceased_AddsTrackingAsActive()
    {
        // Arrange: свежий пользователь, ещё ничего не отслеживает.
        var user = CreateSampleUser();
        var deceasedId = Guid.NewGuid();

        // Act: создаём tracking — родственник, с заметкой и
        // включёнными уведомлениями.
        var result = user.TrackDeceased(
            deceasedId: deceasedId,
            relationshipType: RelationshipType.Relative,
            personalNotes: "Дедушка по отцу",
            notifyOnDeathAnniversary: true,
            notifyOnBirthAnniversary: true);

        // Assert: успех + ровно 1 tracking + статус Active +
        // переданные параметры на месте.
        result.IsSuccess.Should().BeTrue();
        user.TrackedDeceasedItems.Should().HaveCount(1);

        var tracking = user.TrackedDeceasedItems.Single();
        tracking.DeceasedId.Should().Be(deceasedId);
        tracking.Status.Should().Be(TrackStatus.Active);
        tracking.RelationshipType.Should().Be(RelationshipType.Relative);
        tracking.PersonalNotes.Should().Be("Дедушка по отцу");
        tracking.NotifyOnDeathAnniversary.Should().BeTrue();
        tracking.NotifyOnBirthAnniversary.Should().BeTrue();
    }

    /// <summary>
    /// TrackDeceased идемпотентен: если пользователь уже отслеживает
    /// этого deceased (Active/Muted/Archived) — повторный вызов не
    /// создаёт второй tracking, а реактивирует существующий и
    /// обновляет настройки. Ровно так работает кнопка "отслеживать"
    /// в UI: повторный клик → Active. Защищает от дубликатов в БД
    /// (есть ux_tracked_deceased_user_id_deceased_id), но и без них —
    /// домен сам не пускает дубль.
    /// </summary>
    [Fact]
    public void TrackDeceased_SameDeceasedTwice_ReusesExistingTrackingNoDuplicate()
    {
        // Arrange: уже трекаем deceasedX.
        var user = CreateSampleUser();
        var deceasedId = Guid.NewGuid();
        user.TrackDeceased(deceasedId, RelationshipType.Friend);

        // Act: повторно добавляем тот же deceased, но с другими
        // параметрами — родственник вместо друга, плюс уведомления.
        var result = user.TrackDeceased(
            deceasedId: deceasedId,
            relationshipType: RelationshipType.Relative,
            personalNotes: "Уточнение",
            notifyOnDeathAnniversary: true,
            notifyOnBirthAnniversary: false);

        // Assert: успех + всё ещё 1 tracking (НЕ 2!) + параметры
        // обновились до новых значений (Reactivate переписывает).
        result.IsSuccess.Should().BeTrue();
        user.TrackedDeceasedItems.Should().HaveCount(1);

        var tracking = user.TrackedDeceasedItems.Single();
        tracking.RelationshipType.Should().Be(RelationshipType.Relative);
        tracking.PersonalNotes.Should().Be("Уточнение");
        tracking.NotifyOnDeathAnniversary.Should().BeTrue();
        tracking.Status.Should().Be(TrackStatus.Active);
    }

    /// <summary>
    /// Register не пропускает роль SuperAdmin: эту роль выдаёт только
    /// внутренняя фабрика RegisterSuperAdmin (вызываемая из
    /// DbInitializer). Любая попытка зарегистрироваться SuperAdmin'ом
    /// через публичный API должна вернуть RoleInvalid.
    /// </summary>
    [Fact]
    public void Register_AsSuperAdmin_ReturnsRoleInvalid()
    {
        var result = User.Register(
            email: SampleEmail,
            passwordHash: SamplePasswordHash,
            role: UserRole.SuperAdmin);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.role.invalid");
    }

    /// <summary>
    /// ChangePasswordHash при смене пароля обязан обновлять
    /// SecurityStamp — это ключевой механизм инвалидации старых
    /// JWT-токенов. После ChangePasswordHash все ранее выданные
    /// access-токены становятся невалидны при следующей проверке
    /// (см. JwtBearerEvents.OnTokenValidated в API/DependencyInjection.cs).
    /// </summary>
    [Fact]
    public void ChangePasswordHash_NewHash_RotatesSecurityStamp()
    {
        // Arrange
        var user = CreateSampleUser();
        var initialStamp = user.SecurityStamp;

        // Act: меняем пароль.
        var result = user.ChangePasswordHash("new$hash$with$enough$length");

        // Assert: успех + SecurityStamp изменился.
        result.IsSuccess.Should().BeTrue();
        user.SecurityStamp.Should().NotBe(initialStamp);
    }

    /// <summary>
    /// Helper: создаёт минимального валидного пользователя без
    /// дублирования boilerplate.
    /// </summary>
    private static User CreateSampleUser()
    {
        return User.Register(
            email: SampleEmail,
            passwordHash: SamplePasswordHash).Value;
    }
}
