using CSharpFunctionalExtensions;
using System.Net.Mail;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.User;

public class User : Entity<Guid>
{
    public string Email { get; private set; }
    public string UserName { get; private set; }
    public string? FullName { get; private set; }
    public string PasswordHash { get; private set; }
    public DateTime RegisteredAtUtc { get; }
    public DateTime? LastLoginAtUtc { get; private set; }

    private readonly List<TrackedDeceased> _trackedDeceasedItems = new();
    public IReadOnlyCollection<TrackedDeceased> TrackedDeceasedItems => _trackedDeceasedItems.AsReadOnly();

    private User(
        Guid id,
        string email,
        string userName,
        string? fullName,
        string passwordHash,
        DateTime registeredAtUtc) : base(id)
    {
        Email = email;
        UserName = userName;
        FullName = fullName;
        PasswordHash = passwordHash;
        RegisteredAtUtc = registeredAtUtc;
    }

    public static Result<User> Register(
        string email,
        string passwordHash,
        string? fullName = null,
        string? userName = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<User>("Email обязателен");

        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure<User>("Хэш пароля обязателен");

        if (!IsValidEmail(email))
            return Result.Failure<User>("Некорректный email");

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var finalUserName = string.IsNullOrWhiteSpace(userName)
            ? normalizedEmail.Split('@')[0]
            : userName.Trim();

        if (string.IsNullOrWhiteSpace(finalUserName))
            return Result.Failure<User>("Имя пользователя обязательно");

        return Result.Success(new User(
            Guid.NewGuid(),
            normalizedEmail,
            finalUserName,
            string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim(),
            passwordHash,
            DateTime.UtcNow));
    }

    public Result UpdateProfile(string userName, string? fullName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return Result.Failure("Имя пользователя обязательно");

        UserName = userName.Trim();
        FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();

        return Result.Success();
    }

    public Result ChangePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            return Result.Failure("Хэш пароля обязателен");

        PasswordHash = newPasswordHash;
        return Result.Success();
    }

    public Result MarkLogin(DateTime? loggedInAtUtc = null)
    {
        LastLoginAtUtc = loggedInAtUtc ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result<TrackedDeceased> TrackDeceased(
        Guid deceasedId,
        RelationshipType relationshipType,
        string? personalNotes = null,
        bool notifyOnDeathAnniversary = false,
        bool notifyOnBirthAnniversary = false)
    {
        if (_trackedDeceasedItems.Any(x => x.DeceasedId == deceasedId && x.Status != TrackStatus.Archived))
            return Result.Failure<TrackedDeceased>("Этот умерший уже отслеживается пользователем");

        var trackedResult = TrackedDeceased.Create(
            deceasedId,
            relationshipType,
            personalNotes,
            notifyOnDeathAnniversary,
            notifyOnBirthAnniversary);

        if (trackedResult.IsFailure)
            return Result.Failure<TrackedDeceased>(trackedResult.Error);

        _trackedDeceasedItems.Add(trackedResult.Value);
        return Result.Success(trackedResult.Value);
    }

    public Result StopTracking(Guid deceasedId)
    {
        var tracked = _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId && x.Status != TrackStatus.Archived);
        if (tracked is null)
            return Result.Failure("Отслеживание не найдено");

        return tracked.Archive();
    }

    public Result UpdateTracking(
        Guid deceasedId,
        RelationshipType relationshipType,
        string? personalNotes,
        bool notifyOnDeathAnniversary,
        bool notifyOnBirthAnniversary)
    {
        var tracked = _trackedDeceasedItems.FirstOrDefault(x => x.DeceasedId == deceasedId);
        if (tracked is null)
            return Result.Failure("Отслеживание не найдено");

        var relationResult = tracked.UpdateRelationship(relationshipType, personalNotes);
        if (relationResult.IsFailure)
            return relationResult;

        return tracked.ChangeNotifications(notifyOnDeathAnniversary, notifyOnBirthAnniversary);
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