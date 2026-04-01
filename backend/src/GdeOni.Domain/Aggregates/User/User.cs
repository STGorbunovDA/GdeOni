using CSharpFunctionalExtensions;
using System.Net.Mail;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.User;

public sealed class User : Entity<Guid>
{
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
        if (string.IsNullOrWhiteSpace(email))
            return Errors.User.EmailRequired();

        if (string.IsNullOrWhiteSpace(passwordHash))
            return Errors.User.PasswordHashRequired();

        if (!IsValidEmail(email))
            return Errors.User.EmailInvalid();

        if (!Enum.IsDefined(typeof(UserRole), role) || role == UserRole.Unknown || role == UserRole.SuperAdmin)
            return Errors.User.RoleInvalid();

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var finalUserName = string.IsNullOrWhiteSpace(userName)
            ? normalizedEmail.Split('@')[0]
            : userName.Trim();
        
        var normalizedUserName = finalUserName.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(finalUserName))
            return Errors.User.UserNameRequired();

        return Result.Success<User, Error>(new User(
            Guid.NewGuid(),
            normalizedEmail,
            normalizedUserName,
            string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim(),
            passwordHash,
            role,
            DateTime.UtcNow));
    }

    public UnitResult<Error> UpdateProfile(string userName, string? fullName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return Errors.User.UserNameRequired();

        UserName = userName.Trim();
        FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ChangeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Errors.User.EmailRequired();

        if (!IsValidEmail(email))
            return Errors.User.EmailInvalid();

        Email = email.Trim().ToLowerInvariant();
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
        if (!Enum.IsDefined(typeof(UserRole), role) || role == UserRole.Unknown || role == UserRole.SuperAdmin)
            return Errors.User.RoleInvalid();

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
        if (_trackedDeceasedItems.Any(x => x.DeceasedId == deceasedId && x.Status != TrackStatus.Archived))
            return Errors.Tracking.AlreadyTracked();

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

    public TrackedDeceased? GetTracking(Guid deceasedId) =>
        _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId);

    public bool IsTracking(Guid deceasedId) =>
        _trackedDeceasedItems.Any(x => x.DeceasedId == deceasedId && x.Status != TrackStatus.Archived);

    public UnitResult<Error> StopTracking(Guid deceasedId)
    {
        var tracked = _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId && x.Status != TrackStatus.Archived);
        if (tracked is null)
            return Errors.Tracking.NotFound(deceasedId);

        return tracked.Archive();
    }

    public UnitResult<Error> MuteTracking(Guid deceasedId)
    {
        var tracked = _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId);
        if (tracked is null)
            return Errors.Tracking.NotFound(deceasedId);

        return tracked.Mute();
    }

    public UnitResult<Error> ActivateTracking(Guid deceasedId)
    {
        var tracked = _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId);
        if (tracked is null)
            return Errors.Tracking.NotFound(deceasedId);

        return tracked.Activate();
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

        return tracked.ChangeNotifications(notifyOnDeathAnniversary, notifyOnBirthAnniversary);
    }
    
    public UnitResult<Error> RemoveTracking(Guid deceasedId)
    {
        var tracked = _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId);
        if (tracked is null)
            return Errors.Tracking.NotFound(deceasedId);

        _trackedDeceasedItems.Remove(tracked);
        return UnitResult.Success<Error>();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}