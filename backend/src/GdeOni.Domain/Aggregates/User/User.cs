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

        UserName = userNameResult.Value.Display;
        UserNameNormalized = userNameResult.Value.Normalized;
        FullName = fullNameResult.Value;
        Touch();

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ChangeEmail(string email)
    {
        var emailResult = NormalizeEmail(email);
        if (emailResult.IsFailure)
            return emailResult.Error;

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
        return status switch
        {
            TrackStatus.Active => ActivateTracking(deceasedId),
            TrackStatus.Muted => MuteTracking(deceasedId),
            TrackStatus.Archived => StopTracking(deceasedId),
            _ => Errors.Tracking.TrackStatusTypeInvalid()
        };
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

        return tracked.ChangeNotifications(
            notifyOnDeathAnniversary,
            notifyOnBirthAnniversary);
    }

    public UnitResult<Error> RemoveTracking(Guid deceasedId)
    {
        var tracked = _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId);
        if (tracked is null)
            return Errors.Tracking.NotFound(deceasedId);

        _trackedDeceasedItems.Remove(tracked);
        return UnitResult.Success<Error>();
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