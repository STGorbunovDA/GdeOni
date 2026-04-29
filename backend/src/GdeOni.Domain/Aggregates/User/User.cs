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
    public string UserName { get; private set; }
    public string? FullName { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public DateTime RegisteredAtUtc { get; }
    public DateTime? LastLoginAtUtc { get; private set; }

    private readonly List<TrackedDeceased> _trackedDeceasedItems = new();
    public IReadOnlyCollection<TrackedDeceased> TrackedDeceasedItems => _trackedDeceasedItems.AsReadOnly();

    private User() : base(Guid.Empty)
    {
        Email = null!;
        UserName = null!;
        PasswordHash = null!;
        Role = UserRole.Unknown;
    }

    private User(
        Guid id,
        string email,
        string userName,
        string? fullName,
        string passwordHash,
        UserRole role,
        DateTime registeredAtUtc) : base(id)
    {
        Email = email;
        UserName = userName;
        FullName = fullName;
        PasswordHash = passwordHash;
        Role = role;
        RegisteredAtUtc = registeredAtUtc;
    }

    public static Result<User, Error> Register(
        string email,
        string passwordHash,
        string? fullName = null,
        string? userName = null,
        UserRole role = UserRole.RegularUser)
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

        if (!Enum.IsDefined(typeof(UserRole), role) ||
            role == UserRole.Unknown ||
            role == UserRole.SuperAdmin)
        {
            return Errors.User.RoleInvalid();
        }

        return Result.Success<User, Error>(
            new User(
                Guid.NewGuid(),
                emailResult.Value,
                userNameResult.Value,
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

        UserName = userNameResult.Value;
        FullName = fullNameResult.Value;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ChangeEmail(string email)
    {
        var emailResult = NormalizeEmail(email);
        if (emailResult.IsFailure)
            return emailResult.Error;

        Email = emailResult.Value;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ChangePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            return Errors.User.PasswordHashRequired();

        PasswordHash = newPasswordHash;
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
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkLogin(DateTime? loggedInAtUtc = null)
    {
        LastLoginAtUtc = loggedInAtUtc ?? DateTime.UtcNow;
        return UnitResult.Success<Error>();
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

    private static Result<string, Error> NormalizeUserName(string? userName, string normalizedEmail)
    {
        var baseValue = string.IsNullOrWhiteSpace(userName)
            ? normalizedEmail.Split('@')[0]
            : userName.Trim();

        if (string.IsNullOrWhiteSpace(baseValue))
            return Errors.User.UserNameRequired();

        var normalized = baseValue.ToLowerInvariant();

        if (normalized.Length > MaxUserNameLength)
            return Errors.User.UserNameTooLong(MaxUserNameLength);

        return Result.Success<string, Error>(normalized);
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