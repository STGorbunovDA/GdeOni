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

        return Result.Success<PersonName, Error>(new PersonName(
            firstName.Trim(),
            lastName.Trim(),
            string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim()));
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