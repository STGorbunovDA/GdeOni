using CSharpFunctionalExtensions;

namespace GdeOni.Domain.Aggregates.Deceased;

public sealed class PersonName : ValueObject
{
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

    public static Result<PersonName> Create(string firstName, string lastName, string? middleName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Failure<PersonName>("Имя обязательно");

        if (string.IsNullOrWhiteSpace(lastName))
            return Result.Failure<PersonName>("Фамилия обязательна");

        return Result.Success(new PersonName(
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