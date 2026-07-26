using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Tests.UserAggregate;

/// <summary>
/// Тесты <see cref="TrackedDeceased"/> — entity внутри коллекции
/// User.TrackedDeceasedItems. Стандартный сценарий "создал → активен →
/// замьютил уведомления → восстановил → архивировал". Каждый переход
/// статуса проверяется на повторный вызов (AlreadyX-ошибки).
/// </summary>
public sealed class TrackedDeceasedTests
{
    private static readonly Guid SampleDeceasedId = Guid.NewGuid();

    /// <summary>
    /// Create с DeceasedId=Empty: tracking без deceased бессмыслен,
    /// домен ловит как DeceasedIdRequired.
    /// </summary>
    [Fact]
    public void Create_EmptyDeceasedId_ReturnsDeceasedIdRequired()
    {
        var result = TrackedDeceased.Create(
            deceasedId: Guid.Empty,
            relationshipType: RelationshipType.Friend);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tracking.deceased_id.required");
    }

    /// <summary>
    /// Create с RelationshipType вне enum (например, 999):
    /// Enum.IsDefined ловит, домен возвращает RelationshipTypeInvalid.
    /// </summary>
    [Fact]
    public void Create_UndefinedRelationshipType_ReturnsRelationshipTypeInvalid()
    {
        var result = TrackedDeceased.Create(
            deceasedId: SampleDeceasedId,
            relationshipType: (RelationshipType)999);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tracking.relationship_type.invalid");
    }

    /// <summary>
    /// PersonalNotes длиннее MaxPersonalNotesLength → TooLong.
    /// </summary>
    [Fact]
    public void Create_PersonalNotesTooLong_ReturnsTooLong()
    {
        var notes = new string('а', TrackedDeceased.MaxPersonalNotesLength + 1);

        var result = TrackedDeceased.Create(
            deceasedId: SampleDeceasedId,
            relationshipType: RelationshipType.Friend,
            personalNotes: notes);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tracking.personal_notes.too_long");
    }

    /// <summary>
    /// Happy path Create: статус Active, поля сохранены, IsActive() true.
    /// </summary>
    [Fact]
    public void Create_ValidParameters_StartsAsActive()
    {
        var result = TrackedDeceased.Create(
            deceasedId: SampleDeceasedId,
            relationshipType: RelationshipType.Relative,
            personalNotes: "Дядя",
            notifyOnDeathAnniversary: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(TrackStatus.Active);
        result.Value.RelationshipType.Should().Be(RelationshipType.Relative);
        result.Value.PersonalNotes.Should().Be("Дядя");
        result.Value.IsActive().Should().BeTrue();
        result.Value.NotifyOnDeathAnniversary.Should().BeTrue();
        result.Value.NotifyOnBirthAnniversary.Should().BeFalse();
        result.Value.HasNotificationsEnabled().Should().BeTrue();
    }

    /// <summary>
    /// Активировать уже активный → AlreadyActive (Conflict).
    /// </summary>
    [Fact]
    public void Activate_AlreadyActive_ReturnsAlreadyActive()
    {
        var tracking = CreateActiveTracking();

        var result = tracking.Activate();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tracking.already.active");
    }

    /// <summary>
    /// Mute → Mute → AlreadyMuted (повторный mute не идемпотентен —
    /// явная конфликтная ошибка).
    /// </summary>
    [Fact]
    public void Mute_AlreadyMuted_ReturnsAlreadyMuted()
    {
        var tracking = CreateActiveTracking();
        tracking.Mute();

        var result = tracking.Mute();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tracking.already.muted");
    }

    /// <summary>
    /// Archive → Archive → AlreadyArchived. Симметрично Mute / Activate.
    /// </summary>
    [Fact]
    public void Archive_AlreadyArchived_ReturnsAlreadyArchived()
    {
        var tracking = CreateActiveTracking();
        tracking.Archive();

        var result = tracking.Archive();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tracking.already.archived");
    }

    /// <summary>
    /// Reactivate сбрасывает статус в Active независимо от исходного.
    /// Покрываем восстановление из Archived: в реальной жизни это
    /// "снова отслеживать после удаления". Доменная гарантия:
    /// после Reactivate IsActive() == true и notifications обновлены.
    /// </summary>
    [Fact]
    public void Reactivate_FromArchived_BecomesActiveWithNewSettings()
    {
        // Arrange: tracking создали + архивировали.
        var tracking = CreateActiveTracking();
        tracking.Archive();
        tracking.IsArchived().Should().BeTrue();

        // Act: реактивируем с новыми параметрами.
        var result = tracking.Reactivate(
            relationshipType: RelationshipType.Spouse,
            personalNotes: "Супруг(а)",
            notifyOnDeathAnniversary: false,
            notifyOnBirthAnniversary: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tracking.IsActive().Should().BeTrue();
        tracking.RelationshipType.Should().Be(RelationshipType.Spouse);
        tracking.PersonalNotes.Should().Be("Супруг(а)");
        tracking.NotifyOnDeathAnniversary.Should().BeFalse();
        tracking.NotifyOnBirthAnniversary.Should().BeTrue();
    }

    /// <summary>
    /// IsActive / IsMuted / IsArchived — простые предикаты, но
    /// тестируем явно как контракт публичного API tracking'а.
    /// </summary>
    [Fact]
    public void StatusPredicates_ReflectCurrentStatus()
    {
        var tracking = CreateActiveTracking();
        tracking.IsActive().Should().BeTrue();
        tracking.IsMuted().Should().BeFalse();
        tracking.IsArchived().Should().BeFalse();

        tracking.Mute();
        tracking.IsMuted().Should().BeTrue();
        tracking.IsActive().Should().BeFalse();

        tracking.Archive();
        tracking.IsArchived().Should().BeTrue();
        tracking.IsMuted().Should().BeFalse();
    }

    /// <summary>
    /// HasNotificationsEnabled — true если хотя бы один из флагов уведомлений true.
    /// </summary>
    [Fact]
    public void HasNotificationsEnabled_BothFalse_ReturnsFalse()
    {
        var tracking = TrackedDeceased.Create(
            SampleDeceasedId,
            RelationshipType.Friend,
            notifyOnDeathAnniversary: false,
            notifyOnBirthAnniversary: false).Value;

        tracking.HasNotificationsEnabled().Should().BeFalse();
    }

    /// <summary>
    /// F42. Create с булевым флагом notifyDeath=true маппит его в набор
    /// «в день» (0). Флаг обратной совместимости считается из набора.
    /// </summary>
    [Fact]
    public void Create_NotifyDeathTrue_MapsToLeadDayZero()
    {
        var tracking = TrackedDeceased.Create(
            SampleDeceasedId,
            RelationshipType.Friend,
            notifyOnDeathAnniversary: true).Value;

        tracking.DeathAnniversaryLeadDays.Should().Equal(0);
        tracking.BirthAnniversaryLeadDays.Should().BeEmpty();
        tracking.NotifyOnDeathAnniversary.Should().BeTrue();
        tracking.NotifyOnBirthAnniversary.Should().BeFalse();
    }

    /// <summary>
    /// F42. SetAnniversaryReminders задаёт наборы дней; вычисляемые булевы
    /// флаги отражают непустоту набора.
    /// </summary>
    [Fact]
    public void SetAnniversaryReminders_SetsLeadDaysAndComputedFlags()
    {
        var tracking = CreateActiveTracking();

        var result = tracking.SetAnniversaryReminders(
            deathLeadDays: new[] { 0, 7 },
            birthLeadDays: Array.Empty<int>());

        result.IsSuccess.Should().BeTrue();
        tracking.DeathAnniversaryLeadDays.Should().Equal(0, 7);
        tracking.BirthAnniversaryLeadDays.Should().BeEmpty();
        tracking.NotifyOnDeathAnniversary.Should().BeTrue();
        tracking.NotifyOnBirthAnniversary.Should().BeFalse();
    }

    /// <summary>
    /// F42. Набор нормализуется: недопустимые значения отбрасываются, дубли
    /// убираются, порядок сортируется по возрастанию.
    /// </summary>
    [Fact]
    public void SetAnniversaryReminders_NormalizesInput()
    {
        var tracking = CreateActiveTracking();

        tracking.SetAnniversaryReminders(
            deathLeadDays: new[] { 7, 0, 7, 5, 3 }, // 5 недопустимо, 7 дубль
            birthLeadDays: Array.Empty<int>());

        tracking.DeathAnniversaryLeadDays.Should().Equal(0, 3, 7);
    }

    /// <summary>
    /// F42. Совместимость: ChangeNotifications(true) НЕ затирает уже
    /// выбранные на вебе дни (за неделю), а сохраняет их.
    /// </summary>
    [Fact]
    public void ChangeNotifications_TruePreservesExistingLeadDays()
    {
        var tracking = CreateActiveTracking();
        tracking.SetAnniversaryReminders(new[] { 0, 7 }, Array.Empty<int>());

        tracking.ChangeNotifications(
            notifyOnDeathAnniversary: true,
            notifyOnBirthAnniversary: false);

        tracking.DeathAnniversaryLeadDays.Should().Equal(0, 7);
    }

    /// <summary>
    /// F42. Совместимость: ChangeNotifications(true) при пустом наборе
    /// ставит «в день» (0); false — очищает.
    /// </summary>
    [Fact]
    public void ChangeNotifications_TrueFromEmptyDefaultsToLeadDayZero()
    {
        var tracking = CreateActiveTracking();

        tracking.ChangeNotifications(true, true);
        tracking.DeathAnniversaryLeadDays.Should().Equal(0);
        tracking.BirthAnniversaryLeadDays.Should().Equal(0);

        tracking.ChangeNotifications(false, false);
        tracking.DeathAnniversaryLeadDays.Should().BeEmpty();
        tracking.BirthAnniversaryLeadDays.Should().BeEmpty();
        tracking.HasNotificationsEnabled().Should().BeFalse();
    }

    private static TrackedDeceased CreateActiveTracking() =>
        TrackedDeceased.Create(SampleDeceasedId, RelationshipType.Friend).Value;
}
