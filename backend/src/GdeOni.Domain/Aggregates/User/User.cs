using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;
using System.Net.Mail;

namespace GdeOni.Domain.Aggregates.User;

public sealed class User : Entity<Guid>
{
    public const int MaxEmailLength = 320;
    public const int MaxUserNameLength = 100;
    public const int MaxFullNameLength = 300;
    public const int MaxRole = 50;
    public const int MaxPasswordHash = 1000;
    public const int MaxComplimentaryNoteLength = 500;
    public string Email { get; private set; }

    /// <summary>
    /// Display-форма имени пользователя — то, как ввёл сам юзер
    /// (с регистром). Возвращается в /me, JWT, ответы /users/{id} и т.п.
    /// </summary>
    public string UserName { get; private set; }

    /// <summary>
    /// Lowercase-форма UserName для unique-индекса и поиска. Никогда
    /// не показывается клиенту. Поддерживает кейс «JohnDoe vs johndoe
    /// — это один и тот же логин».
    /// </summary>
    public string UserNameNormalized { get; private set; }

    public string? FullName { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public DateTime RegisteredAtUtc { get; }
    public DateTime? LastLoginAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Метка инвалидации JWT. Кладётся в access-токен как claim "stamp".
    /// При смене пароля / роли / email обновляется на новый Guid —
    /// все ранее выпущенные токены становятся невалидны при следующей
    /// проверке (см. JwtBearerEvents.OnTokenValidated).
    /// </summary>
    public Guid SecurityStamp { get; private set; }

    /// <summary>
    /// D16. Owned VO с состоянием подписки. У свежезарегистрированного
    /// юзера = <see cref="Subscription.Initial"/> (Status=None); после
    /// <c>RegisterUserUseCase</c> сразу вызывает <see cref="StartTrial"/>
    /// — переход в Status=Trial с ExpiresAtUtc = now + 30d.
    /// </summary>
    public Subscription Subscription { get; private set; } = Subscription.Initial();

    /// <summary>
    /// D19. Момент принятия Privacy Policy. null = не принято
    /// (пользователь зарегистрировался ещё до введения 152-ФЗ guard'а
    /// или процедуру намеренно обходили; в проде такого быть не должно).
    /// </summary>
    public DateTime? PrivacyPolicyAcceptedAtUtc { get; private set; }

    /// <summary>
    /// D19. Момент принятия Terms of Use. null = не принято.
    /// </summary>
    public DateTime? TermsAcceptedAtUtc { get; private set; }

    /// <summary>
    /// D19. Версия Privacy Policy, принятая пользователем. 0 = не принято.
    /// Сравнивается с актуальной <c>LegalOptions.CurrentPrivacyPolicyVersion</c> —
    /// если меньше, клиент должен показать "Политика обновлена, примите снова".
    /// </summary>
    public int PrivacyPolicyVersion { get; private set; }

    /// <summary>
    /// D19. Версия Terms of Use, принятая пользователем. 0 = не принято.
    /// Сравнивается с <c>LegalOptions.CurrentTermsVersion</c> аналогично
    /// <see cref="PrivacyPolicyVersion"/>.
    /// </summary>
    public int TermsVersion { get; private set; }

    /// <summary>
    /// D22. Момент выдачи бесплатного доступа админом. null = не выдан
    /// (или отозван). Наличие значения = "complimentary активен" (с
    /// учётом UntilUtc).
    /// </summary>
    public DateTime? ComplimentaryAccessGrantedAtUtc { get; private set; }

    /// <summary>
    /// D22. До какого момента действует бесплатный доступ. null при
    /// <see cref="ComplimentaryAccessGrantedAtUtc"/> != null →
    /// бессрочный доступ. Конкретная дата → доступ до этого момента.
    /// </summary>
    public DateTime? ComplimentaryAccessUntilUtc { get; private set; }

    /// <summary>
    /// D22. Id админа, выдавшего бесплатный доступ. Для аудита.
    /// </summary>
    public Guid? ComplimentaryAccessGrantedByAdminId { get; private set; }

    /// <summary>
    /// D22. Опциональная заметка ("друг основателя", "support ticket 42"),
    /// показывается юзеру в SubscriptionPage. Max
    /// <see cref="MaxComplimentaryNoteLength"/> символов.
    /// </summary>
    public string? ComplimentaryAccessNote { get; private set; }

    private readonly List<TrackedDeceased> _trackedDeceasedItems = new();
    public IReadOnlyCollection<TrackedDeceased> TrackedDeceasedItems => _trackedDeceasedItems.AsReadOnly();

    private User() : base(Guid.Empty)
    {
        Email = null!;
        UserName = null!;
        UserNameNormalized = null!;
        PasswordHash = null!;
        Role = UserRole.Unknown;
    }

    private User(
        Guid id,
        string email,
        string userName,
        string userNameNormalized,
        string? fullName,
        string passwordHash,
        UserRole role,
        DateTime registeredAtUtc) : base(id)
    {
        Email = email;
        UserName = userName;
        UserNameNormalized = userNameNormalized;
        FullName = fullName;
        PasswordHash = passwordHash;
        Role = role;
        RegisteredAtUtc = registeredAtUtc;
        SecurityStamp = Guid.NewGuid();
        Subscription = Subscription.Initial();
    }

    public static Result<User, Error> Register(
        string email,
        string passwordHash,
        string? fullName = null,
        string? userName = null,
        UserRole role = UserRole.RegularUser)
    {
        if (!Enum.IsDefined(typeof(UserRole), role) ||
            role == UserRole.Unknown ||
            role == UserRole.SuperAdmin)
        {
            return Errors.User.RoleInvalid();
        }

        return BuildUser(email, passwordHash, fullName, userName, role);
    }

    /// <summary>
    /// Привилегированная фабрика для seed-сценария: создаёт пользователя
    /// с ролью SuperAdmin. Используется только из инфраструктуры
    /// (DbInitializer). Через публичный API роль SuperAdmin недостижима —
    /// Register отвергает её, ChangeRole тоже.
    /// </summary>
    public static Result<User, Error> RegisterSuperAdmin(
        string email,
        string passwordHash,
        string? fullName = null,
        string? userName = null)
    {
        return BuildUser(email, passwordHash, fullName, userName, UserRole.SuperAdmin);
    }

    private static Result<User, Error> BuildUser(
        string email,
        string passwordHash,
        string? fullName,
        string? userName,
        UserRole role)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            return Errors.User.PasswordHashRequired();

        var emailResult = NormalizeEmail(email);
        if (emailResult.IsFailure)
            return emailResult.Error;

        var userNameResult = NormalizeUserName(userName, emailResult.Value);
        if (userNameResult.IsFailure)
            return userNameResult.Error;

        var fullNameResult = NormalizeFullName(fullName);
        if (fullNameResult.IsFailure)
            return fullNameResult.Error;

        return Result.Success<User, Error>(
            new User(
                Guid.NewGuid(),
                emailResult.Value,
                userNameResult.Value.Display,
                userNameResult.Value.Normalized,
                fullNameResult.Value,
                passwordHash,
                role,
                DateTime.UtcNow));
    }

    public UnitResult<Error> UpdateProfile(string userName, string? fullName)
    {
        var userNameResult = NormalizeUserName(userName, Email);
        if (userNameResult.IsFailure)
            return userNameResult.Error;

        var fullNameResult = NormalizeFullName(fullName);
        if (fullNameResult.IsFailure)
            return fullNameResult.Error;

        // No-op guard (D11.8.2): PATCH с теми же значениями не должен
        // ротировать SecurityStamp и инвалидировать токены на других
        // устройствах. Сравниваем по нормализованным формам.
        if (UserNameNormalized == userNameResult.Value.Normalized &&
            FullName == fullNameResult.Value)
        {
            return UnitResult.Success<Error>();
        }

        UserName = userNameResult.Value.Display;
        UserNameNormalized = userNameResult.Value.Normalized;
        FullName = fullNameResult.Value;
        // UserName уходит в JWT-claim ClaimTypes.Name (см. JwtProvider:27),
        // поэтому смена имени должна инвалидировать ранее выпущенные
        // access-токены — ротируем SecurityStamp как при ChangeEmail.
        // (D11.4.2)
        SecurityStamp = Guid.NewGuid();
        Touch();

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ChangeEmail(string email)
    {
        var emailResult = NormalizeEmail(email);
        if (emailResult.IsFailure)
            return emailResult.Error;

        // No-op guard (D11.8.2): тот же email после нормализации —
        // не ротируем SecurityStamp.
        if (Email == emailResult.Value)
            return UnitResult.Success<Error>();

        Email = emailResult.Value;
        SecurityStamp = Guid.NewGuid();
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ChangePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            return Errors.User.PasswordHashRequired();

        PasswordHash = newPasswordHash;
        SecurityStamp = Guid.NewGuid();
        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ChangeRole(UserRole role)
    {
        if (!Enum.IsDefined(typeof(UserRole), role) ||
            role == UserRole.Unknown ||
            role == UserRole.SuperAdmin)
        {
            return Errors.User.RoleInvalid();
        }

        // No-op guard (D11.8.2): та же роль — не ротируем SecurityStamp
        // и не дёргаем RevokeAllForUser. Защита от случайного
        // "переназначить ту же роль через UI" → массовый force-logout.
        if (Role == role)
            return UnitResult.Success<Error>();

        Role = role;
        SecurityStamp = Guid.NewGuid();
        Touch();
        return UnitResult.Success<Error>();
    }

    public void MarkLogin(DateTime? loggedInAtUtc = null)
    {
        LastLoginAtUtc = loggedInAtUtc ?? DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    /// D16. Стартует пробный период. Вызывается из
    /// <c>RegisterUserUseCase</c> сразу после <see cref="Register"/>.
    /// Idempotent: если подписка уже не в Initial-состоянии — no-op
    /// (защита от повторных вызовов / миграционных сценариев).
    /// </summary>
    public UnitResult<Error> StartTrial(DateTime nowUtc, TimeSpan trialDuration)
    {
        if (trialDuration <= TimeSpan.Zero)
            return Errors.Subscription.TrialDurationInvalid();

        if (Subscription.Status != SubscriptionStatus.None)
            return UnitResult.Success<Error>();

        Subscription = Subscription.WithTrial(nowUtc, trialDuration);
        Touch();
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Админский restart триала: переводит юзера в Trial с новым сроком
    /// независимо от текущего статуса. Используется когда админ хочет
    /// вернуть юзеру бесплатный пробный период (например, для
    /// поддержки после жалобы).
    /// </summary>
    public UnitResult<Error> RestartTrialByAdmin(DateTime nowUtc, TimeSpan trialDuration)
    {
        if (trialDuration <= TimeSpan.Zero)
            return Errors.Subscription.TrialDurationInvalid();

        Subscription = Subscription.WithTrial(nowUtc, trialDuration);
        Touch();
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// D16. Помечает, что пользователь инициировал оплату — webhook
    /// от YooKassa может прийти спустя несколько секунд/минут. Хранит
    /// externalPaymentId, по которому потом найдём этого юзера в
    /// <c>ProcessPaymentWebhook</c>.
    /// </summary>
    public UnitResult<Error> RequestSubscriptionPayment(SubscriptionPlan plan, string paymentId)
    {
        if (!Enum.IsDefined(typeof(SubscriptionPlan), plan))
            return Errors.Subscription.PlanInvalid();

        if (string.IsNullOrWhiteSpace(paymentId))
            return Errors.Subscription.PaymentIdRequired();

        if (paymentId.Length > Subscription.MaxPaymentIdLength)
            return Errors.Subscription.PaymentIdTooLong(Subscription.MaxPaymentIdLength);

        // Защита от случайной двойной оплаты на Active без отмены —
        // фронту лучше показать "Подписка уже активна" вместо двойного
        // списания.
        if (Subscription.Status == SubscriptionStatus.Active)
            return Errors.Subscription.AlreadyActive();

        Subscription = Subscription.WithPendingPayment(plan, paymentId);
        Touch();
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// D16. Активирует подписку после успешного webhook'а YooKassa.
    /// Idempotent: если уже Active с тем же или более поздним
    /// ExpiresAtUtc — no-op. Если новый ExpiresAtUtc позже текущего —
    /// продлеваем.
    /// </summary>
    public UnitResult<Error> ActivateSubscription(
        SubscriptionPlan plan,
        DateTime nowUtc,
        DateTime expiresAtUtc,
        string paymentId)
    {
        if (!Enum.IsDefined(typeof(SubscriptionPlan), plan))
            return Errors.Subscription.PlanInvalid();

        if (string.IsNullOrWhiteSpace(paymentId))
            return Errors.Subscription.PaymentIdRequired();

        if (paymentId.Length > Subscription.MaxPaymentIdLength)
            return Errors.Subscription.PaymentIdTooLong(Subscription.MaxPaymentIdLength);

        if (expiresAtUtc <= nowUtc)
            return Errors.Subscription.ExpiresAtInPast();

        // Идемпотентность для retry webhook: тот же payment, не двигает
        // ExpiresAtUtc вперёд → no-op. Это закрывает кейс "webhook
        // YooKassa пришёл дважды".
        if (Subscription.Status == SubscriptionStatus.Active
            && Subscription.LastPaymentId == paymentId
            && Subscription.ExpiresAtUtc is { } currentExpiry
            && expiresAtUtc <= currentExpiry)
        {
            return UnitResult.Success<Error>();
        }

        var currentPeriodStart = Subscription.Status == SubscriptionStatus.Active
            && Subscription.CurrentPeriodStartedAtUtc is { } existing
                ? existing
                : nowUtc;

        Subscription = Subscription.WithActive(plan, currentPeriodStart, expiresAtUtc, paymentId);
        Touch();
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// D16. Отмена подписки пользователем. ExpiresAtUtc сохраняется —
    /// доступ дорабатывает до конца paid-period.
    /// </summary>
    public UnitResult<Error> CancelSubscription(DateTime nowUtc)
    {
        // Отменить можно только то, что сейчас даёт доступ — Trial
        // или Active. Остальные статусы — no-op для безопасности.
        if (Subscription.Status is not SubscriptionStatus.Trial
            and not SubscriptionStatus.Active)
        {
            return Errors.Subscription.NotCancellable();
        }

        Subscription = Subscription.WithCancelled(nowUtc);
        Touch();
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Админский revoke: моментально снимает подписку (Expired с
    /// expires=now), независимо от текущего статуса. No-op для тех,
    /// у кого подписки не было (None).
    /// </summary>
    public UnitResult<Error> RevokeSubscriptionByAdmin(DateTime nowUtc)
    {
        if (Subscription.Status == SubscriptionStatus.None)
            return UnitResult.Success<Error>();

        Subscription = Subscription.WithRevoked(nowUtc);
        Touch();
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// D16. Главный гейт-метод: пускать ли пользователя на endpoint'ы,
    /// требующие активной подписки. Учитывает Trial / Active /
    /// Cancelled (последний — пока ExpiresAtUtc не наступил).
    /// gracePeriodDays добавляется к ExpiresAtUtc — на случай задержки
    /// webhook YooKassa при автосписании.
    /// </summary>
    public bool HasActiveSubscription(DateTime nowUtc, int gracePeriodDays = 0) =>
        Subscription.IsActive(nowUtc, gracePeriodDays);

    /// <summary>
    /// D16. true только если сейчас Trial. Используется UI для показа
    /// баннера "Пробный период до DD.MM.YYYY".
    /// </summary>
    public bool IsOnTrial(DateTime nowUtc) => Subscription.IsOnTrial(nowUtc);

    /// <summary>
    /// D19. Фиксирует согласие с Privacy Policy и Terms of Use на
    /// указанных версиях. Вызывается из <c>RegisterUserUseCase</c>
    /// сразу после <see cref="Register"/>, а также из
    /// <c>AcceptLegalUseCase</c> когда юзер пере-принимает обновлённую
    /// версию документов.
    ///
    /// No-op guard: если те же версии уже приняты, ничего не делает —
    /// не сбивает таймстамп и не дёргает <c>Touch()</c>. Это защищает
    /// от лишних UPDATE при повторных запросах с клиента, который не
    /// обнаружил что версия не изменилась.
    /// </summary>
    public UnitResult<Error> AcceptLegal(
        int privacyPolicyVersion,
        int termsVersion,
        DateTime nowUtc)
    {
        if (privacyPolicyVersion < 1)
            return Errors.Legal.PrivacyPolicyVersionInvalid();

        if (termsVersion < 1)
            return Errors.Legal.TermsVersionInvalid();

        if (PrivacyPolicyVersion == privacyPolicyVersion
            && TermsVersion == termsVersion)
        {
            return UnitResult.Success<Error>();
        }

        PrivacyPolicyAcceptedAtUtc = nowUtc;
        TermsAcceptedAtUtc = nowUtc;
        PrivacyPolicyVersion = privacyPolicyVersion;
        TermsVersion = termsVersion;
        Touch();
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// D19. true если у пользователя принятая версия Privacy Policy
    /// или Terms старше текущей конфигурируемой. Используется
    /// frontend'ом (через DTO в /me) для показа модалки "Документы
    /// обновлены, прочитайте и подтвердите".
    /// </summary>
    public bool HasOutdatedLegalAcceptance(int currentPrivacyVersion, int currentTermsVersion) =>
        PrivacyPolicyVersion < currentPrivacyVersion
        || TermsVersion < currentTermsVersion;

    /// <summary>
    /// D22. Выдаёт бесплатный доступ ко всему приложению независимо от
    /// подписки. <paramref name="untilUtc"/> = null → бессрочно;
    /// иначе доступ до этого момента (должен быть в будущем).
    ///
    /// No-op guard: если те же значения уже выставлены, ничего не делает.
    /// SecurityStamp НЕ ротируется — это admin-action, юзер не должен
    /// принудительно перелогиниться при выдаче доступа.
    /// </summary>
    public UnitResult<Error> GrantComplimentaryAccess(
        Guid adminId,
        DateTime? untilUtc,
        string? note,
        DateTime nowUtc)
    {
        if (adminId == Guid.Empty)
            return Errors.Complimentary.AdminIdRequired();

        if (untilUtc.HasValue && untilUtc.Value <= nowUtc)
            return Errors.Complimentary.UntilDateInPast();

        var normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (normalizedNote is not null && normalizedNote.Length > MaxComplimentaryNoteLength)
            return Errors.Complimentary.NoteTooLong(MaxComplimentaryNoteLength);

        // No-op guard (см. D11.14): те же untilUtc + note → не двигаем
        // GrantedAtUtc и не дёргаем Touch. GrantedByAdminId намеренно
        // не входит в сравнение — если кнопку нажал тот же админ
        // повторно, это полная no-op; если другой админ с теми же
        // параметрами — тоже no-op (последний "автор" не важен).
        if (ComplimentaryAccessGrantedAtUtc.HasValue
            && ComplimentaryAccessUntilUtc == untilUtc
            && ComplimentaryAccessNote == normalizedNote)
        {
            return UnitResult.Success<Error>();
        }

        ComplimentaryAccessGrantedAtUtc = nowUtc;
        ComplimentaryAccessUntilUtc = untilUtc;
        ComplimentaryAccessGrantedByAdminId = adminId;
        ComplimentaryAccessNote = normalizedNote;
        Touch();
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// D22. Отзывает бесплатный доступ — обнуляет 4 поля. Если доступа
    /// не было — no-op без Touch.
    /// </summary>
    public UnitResult<Error> RevokeComplimentaryAccess()
    {
        if (!ComplimentaryAccessGrantedAtUtc.HasValue)
            return UnitResult.Success<Error>();

        ComplimentaryAccessGrantedAtUtc = null;
        ComplimentaryAccessUntilUtc = null;
        ComplimentaryAccessGrantedByAdminId = null;
        ComplimentaryAccessNote = null;
        Touch();
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// D22. true если юзеру выдан бесплатный доступ и он ещё действует.
    /// Три ветки: не выдан → false; UntilUtc=null → бессрочно (true);
    /// UntilUtc &gt; nowUtc → true; иначе → false (истёк).
    /// </summary>
    public bool HasComplimentaryAccess(DateTime nowUtc)
    {
        if (!ComplimentaryAccessGrantedAtUtc.HasValue)
            return false;

        if (!ComplimentaryAccessUntilUtc.HasValue)
            return true;

        return ComplimentaryAccessUntilUtc.Value > nowUtc;
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Result<TrackedDeceased, Error> TrackDeceased(
        Guid deceasedId,
        RelationshipType relationshipType,
        string? personalNotes = null,
        bool notifyOnDeathAnniversary = false,
        bool notifyOnBirthAnniversary = false)
    {
        var existingTracking = _trackedDeceasedItems
            .FirstOrDefault(x => x.DeceasedId == deceasedId);

        if (existingTracking is not null)
        {
            var reactivateResult = existingTracking.Reactivate(
                relationshipType,
                personalNotes,
                notifyOnDeathAnniversary,
                notifyOnBirthAnniversary);

            if (reactivateResult.IsFailure)
                return reactivateResult.Error;

            Touch();
            return Result.Success<TrackedDeceased, Error>(existingTracking);
        }

        var trackedResult = TrackedDeceased.Create(
            deceasedId,
            relationshipType,
            personalNotes,
            notifyOnDeathAnniversary,
            notifyOnBirthAnniversary);

        if (trackedResult.IsFailure)
            return trackedResult.Error;

        _trackedDeceasedItems.Add(trackedResult.Value);
        Touch();
        return Result.Success<TrackedDeceased, Error>(trackedResult.Value);
    }

    public Result<TrackStatus, Error> GetTrackingStatus(Guid deceasedId)
    {
        var tracked = _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId);
        if (tracked is null)
            return Errors.Tracking.NotFound(deceasedId);

        return Result.Success<TrackStatus, Error>(tracked.Status);
    }

    public TrackedDeceased? GetTracking(Guid deceasedId) =>
        _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId);

    public UnitResult<Error> ChangeTrackingStatus(Guid deceasedId, TrackStatus status)
    {
        var result = status switch
        {
            TrackStatus.Active => ActivateTracking(deceasedId),
            TrackStatus.Muted => MuteTracking(deceasedId),
            TrackStatus.Archived => StopTracking(deceasedId),
            _ => Errors.Tracking.TrackStatusTypeInvalid()
        };

        if (result.IsSuccess)
            Touch();

        return result;
    }

    public UnitResult<Error> UpdateTracking(
        Guid deceasedId,
        RelationshipType relationshipType,
        string? personalNotes,
        bool notifyOnDeathAnniversary,
        bool notifyOnBirthAnniversary)
    {
        var tracked = _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId);
        if (tracked is null)
            return Errors.Tracking.NotFound(deceasedId);

        var relationResult = tracked.UpdateRelationship(relationshipType, personalNotes);
        if (relationResult.IsFailure)
            return relationResult.Error;

        var notificationsResult = tracked.ChangeNotifications(
            notifyOnDeathAnniversary,
            notifyOnBirthAnniversary);

        if (notificationsResult.IsFailure)
            return notificationsResult;

        Touch();
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> RemoveTracking(Guid deceasedId)
    {
        var tracked = _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId);
        if (tracked is null)
            return Errors.Tracking.NotFound(deceasedId);

        _trackedDeceasedItems.Remove(tracked);
        Touch();
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Снимает все отслеживания юзера разом. Возвращает количество
    /// удалённых записей. Используется админом для bulk-операции.
    /// </summary>
    public int RemoveAllTracking()
    {
        var count = _trackedDeceasedItems.Count;
        if (count == 0) return 0;
        _trackedDeceasedItems.Clear();
        Touch();
        return count;
    }

    private UnitResult<Error> StopTracking(Guid deceasedId)
    {
        var tracked = _trackedDeceasedItems
            .FirstOrDefault(x => x.DeceasedId == deceasedId && x.Status != TrackStatus.Archived);

        if (tracked is null)
            return Errors.Tracking.NotFound(deceasedId);

        return tracked.Archive();
    }

    private UnitResult<Error> MuteTracking(Guid deceasedId)
    {
        var tracked = _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId);
        if (tracked is null)
            return Errors.Tracking.NotFound(deceasedId);

        return tracked.Mute();
    }

    private UnitResult<Error> ActivateTracking(Guid deceasedId)
    {
        var tracked = _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId);
        if (tracked is null)
            return Errors.Tracking.NotFound(deceasedId);

        return tracked.Activate();
    }

    private static Result<string, Error> NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Errors.User.EmailRequired();

        var normalized = email.Trim().ToLowerInvariant();

        if (normalized.Length > MaxEmailLength)
            return Errors.User.EmailTooLong(MaxEmailLength);

        if (!IsValidEmail(normalized))
            return Errors.User.EmailInvalid();

        return Result.Success<string, Error>(normalized);
    }

    private static Result<(string Display, string Normalized), Error> NormalizeUserName(
        string? userName,
        string normalizedEmail)
    {
        // Если юзер не передал UserName — берём prefix email'а (он уже
        // в lowercase после NormalizeEmail). В этом случае Display и
        // Normalized совпадают.
        var display = string.IsNullOrWhiteSpace(userName)
            ? normalizedEmail.Split('@')[0]
            : userName.Trim();

        if (string.IsNullOrWhiteSpace(display))
            return Errors.User.UserNameRequired();

        if (display.Length > MaxUserNameLength)
            return Errors.User.UserNameTooLong(MaxUserNameLength);

        return Result.Success<(string, string), Error>(
            (display, display.ToLowerInvariant()));
    }

    /// <summary>
    /// Единый источник истины для нормализованной формы UserName,
    /// используемой в unique-индексе (D11.8.3). Use case'ы зовут
    /// этот helper для проверки `ExistsByUserName(normalized)` —
    /// иначе при изменении правил нормализации в домене (strip эмодзи,
    /// NFKC и т.д.) Application-слой продолжит проверять по старой
    /// логике и конфликт всплывёт только через 23505 на Save.
    /// При невалидном имени возвращает Failure (не Domain Error,
    /// а просто пустую строку — use case всё равно не может сделать
    /// проверку и должен дальше отдать запрос в User.Register, который
    /// вернёт нормальную ошибку).
    /// </summary>
    public static string ComputeNormalizedUserName(string? userName, string email)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        var display = string.IsNullOrWhiteSpace(userName)
            ? (normalizedEmail.Length > 0 ? normalizedEmail.Split('@')[0] : string.Empty)
            : userName.Trim();
        return display.ToLowerInvariant();
    }

    private static Result<string?, Error> NormalizeFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return Result.Success<string?, Error>(null);

        var normalized = fullName.Trim();

        if (normalized.Length > MaxFullNameLength)
            return Errors.User.FullNameTooLong(MaxFullNameLength);

        return Result.Success<string?, Error>(normalized);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }
}