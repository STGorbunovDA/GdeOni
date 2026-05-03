using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.DeceasedRecords;

public sealed class PersonName : ValueObject
{
    public const int MaxFirstName = 200;
    public const int MaxLastName = 200;
    public const int MaxMiddleName = 200;
    
    public string FirstName { get; }
    public string LastName { get; }
    public string? MiddleName { get; }

    private PersonName()
    {
        FirstName = null!;
        LastName = null!;
    }

    private PersonName(string firstName, string lastName, string? middleName)
    {
        FirstName = firstName;
        LastName = lastName;
        MiddleName = middleName;
    }

    public static Result<PersonName, Error> Create(string firstName, string lastName, string? middleName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return Errors.PersonName.FirstNameRequired();

        if (string.IsNullOrWhiteSpace(lastName))
            return Errors.PersonName.LastNameRequired();

        var normalizedFirstName = firstName.Trim();
        if (normalizedFirstName.Length > MaxFirstName)
            return Errors.PersonName.FirstNameTooLong(MaxFirstName);

        var normalizedLastName = lastName.Trim();
        if (normalizedLastName.Length > MaxLastName)
            return Errors.PersonName.LastNameTooLong(MaxLastName);

        var normalizedMiddleName = string.IsNullOrWhiteSpace(middleName)
            ? null
            : middleName.Trim();
        if (normalizedMiddleName is not null && normalizedMiddleName.Length > MaxMiddleName)
            return Errors.PersonName.MiddleNameTooLong(MaxMiddleName);

        return Result.Success<PersonName, Error>(new PersonName(
            normalizedFirstName,
            normalizedLastName,
            normalizedMiddleName));
    }

    public string FullName =>
        string.Join(" ", new[] { LastName, FirstName, MiddleName }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
        yield return MiddleName ?? string.Empty;
    }
}