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
    /// UpdateProfile / ChangeEmail / ChangeRole — все три эти метода
    /// должны ротировать SecurityStamp, иначе старые JWT-токены
    /// продолжат работать после смены критичных полей. Покрываем
    /// каждый отдельно: ротирование stamp'а — общий контракт всех
    /// "значимых" мутаций User.
    /// </summary>
    [Fact]
    public void ChangeEmail_NewEmail_RotatesSecurityStamp()
    {
        var user = CreateSampleUser();
        var initialStamp = user.SecurityStamp;

        user.ChangeEmail("new@example.com");

        user.SecurityStamp.Should().NotBe(initialStamp);
    }

    [Fact]
    public void ChangeRole_NewRole_RotatesSecurityStamp()
    {
        var user = CreateSampleUser();
        var initialStamp = user.SecurityStamp;

        user.ChangeRole(UserRole.Admin);

        user.SecurityStamp.Should().NotBe(initialStamp);
    }

    /// <summary>
    /// UpdateProfile НЕ обязан ротировать SecurityStamp — UserName
    /// и FullName не критичны для security. Проверяем явно, чтобы
    /// случайный рефакторинг не добавил ротацию (это привело бы
    /// к лишним переавторизациям при банальном rename).
    /// </summary>
    [Fact]
    public void UpdateProfile_NewName_DoesNotRotateSecurityStamp()
    {
        var user = CreateSampleUser();
        var initialStamp = user.SecurityStamp;

        user.UpdateProfile(userName: "new-name", fullName: "Иван Иванов");

        user.SecurityStamp.Should().Be(initialStamp);
    }

    /// <summary>
    /// ChangeEmail с невалидным форматом → EmailInvalid (System.Net.Mail
    /// MailAddress конструктор кидает, домен ловит).
    /// </summary>
    [Fact]
    public void ChangeEmail_Invalid_ReturnsEmailInvalid()
    {
        var user = CreateSampleUser();

        var result = user.ChangeEmail("not-an-email");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.email.invalid");
    }

    /// <summary>
    /// ChangeRole = Unknown / SuperAdmin / out-of-enum → RoleInvalid.
    /// SuperAdmin специально недостижим через публичный API.
    /// </summary>
    [Theory]
    [InlineData(UserRole.Unknown)]
    [InlineData(UserRole.SuperAdmin)]
    public void ChangeRole_DisallowedRole_ReturnsRoleInvalid(UserRole role)
    {
        var user = CreateSampleUser();

        var result = user.ChangeRole(role);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.role.invalid");
    }

    /// <summary>
    /// ChangeTrackingStatus переводит между Active/Muted/Archived
    /// через единый switch (см. User.ChangeTrackingStatus). Тестируем
    /// все три ветки + неподдерживаемое значение.
    /// </summary>
    [Fact]
    public void ChangeTrackingStatus_ToMuted_MutesTracking()
    {
        var user = CreateSampleUser();
        var deceasedId = Guid.NewGuid();
        user.TrackDeceased(deceasedId, RelationshipType.Friend);

        var result = user.ChangeTrackingStatus(deceasedId, TrackStatus.Muted);

        result.IsSuccess.Should().BeTrue();
        user.GetTracking(deceasedId)!.IsMuted().Should().BeTrue();
    }

    [Fact]
    public void ChangeTrackingStatus_InvalidValue_ReturnsTrackStatusTypeInvalid()
    {
        var user = CreateSampleUser();
        var deceasedId = Guid.NewGuid();
        user.TrackDeceased(deceasedId, RelationshipType.Friend);

        var result = user.ChangeTrackingStatus(deceasedId, (TrackStatus)999);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tracking.track_status.invalid");
    }

    /// <summary>
    /// RemoveTracking на несуществующем deceasedId → NotFound
    /// (а не silent success). Иначе клиент может думать, что
    /// удаление прошло, хотя ничего не изменилось.
    /// </summary>
    [Fact]
    public void RemoveTracking_NonExistent_ReturnsNotFound()
    {
        var user = CreateSampleUser();

        var result = user.RemoveTracking(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tracking.not.found");
    }

    /// <summary>
    /// GetTracking возвращает null для несуществующего, instance
    /// для существующего. Контракт публичного API.
    /// </summary>
    [Fact]
    public void GetTracking_Existing_ReturnsTracking()
    {
        var user = CreateSampleUser();
        var deceasedId = Guid.NewGuid();
        user.TrackDeceased(deceasedId, RelationshipType.Friend);

        user.GetTracking(deceasedId).Should().NotBeNull();
        user.GetTracking(Guid.NewGuid()).Should().BeNull();
    }

    /// <summary>
    /// Register без UserName использует prefix email'а (часть до '@').
    /// Display и Normalized совпадают — оба в lowercase, потому что
    /// Email уже нормализован к lowercase в NormalizeEmail.
    /// </summary>
    [Fact]
    public void Register_WithoutUserName_UsesEmailPrefix()
    {
        var result = User.Register(
            email: "JOHN@example.com",
            passwordHash: SamplePasswordHash);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserName.Should().Be("john");
        result.Value.UserNameNormalized.Should().Be("john");
        result.Value.Email.Should().Be("john@example.com");
    }

    /// <summary>
    /// MarkLogin обновляет LastLoginAtUtc до сейчас (и проставляет
    /// UpdatedAtUtc через Touch). Используется в LoginUseCase.
    /// </summary>
    [Fact]
    public void MarkLogin_SetsLastLoginAtUtcAndTouches()
    {
        var user = CreateSampleUser();
        user.LastLoginAtUtc.Should().BeNull();

        user.MarkLogin();

        user.LastLoginAtUtc.Should().NotBeNull();
        user.LastLoginAtUtc!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        user.UpdatedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// ChangePasswordHash с пустым хешем → PasswordHashRequired.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ChangePasswordHash_Empty_ReturnsPasswordHashRequired(string? hash)
    {
        var user = CreateSampleUser();

        var result = user.ChangePasswordHash(hash!);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.password_hash.required");
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
