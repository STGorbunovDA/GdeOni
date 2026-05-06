using GdeOni.Domain.Aggregates.DeceasedRecords;

namespace GdeOni.Domain.Tests.Aggregates.DeceasedRecords;

/// <summary>
/// Тесты value object'а <see cref="PersonName"/>. PersonName — пара
/// (FirstName, LastName) + опциональный MiddleName, нормализуется
/// trim'ом, валидируется по обязательности и MaxLength. Используется
/// и в Deceased.Name, и потенциально в User.FullName.
/// </summary>
public sealed class PersonNameTests
{
    /// <summary>
    /// FirstName обязателен. Пустая строка / whitespace / null
    /// одинаково отвергаются: домен не может построить корректный
    /// SearchKey без имени, поэтому ловим заранее.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_FirstNameMissing_ReturnsFirstNameRequired(string? firstName)
    {
        var result = PersonName.Create(firstName!, lastName: "Иванов", middleName: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("person_name.first_name.required");
    }

    /// <summary>
    /// LastName обязателен — те же три кейса. Без фамилии человек
    /// в каталоге не идентифицируется (полные тёзки), плюс ломается
    /// SearchKey-уникальность.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_LastNameMissing_ReturnsLastNameRequired(string? lastName)
    {
        var result = PersonName.Create(firstName: "Иван", lastName: lastName!, middleName: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("person_name.last_name.required");
    }

    /// <summary>
    /// FirstName длиннее MaxFirstName (200) — TooLong-ошибка.
    /// Проверяем на 201 символе ровно одним больше лимита, чтобы
    /// убедиться что граница включает 200 (не 201).
    /// </summary>
    [Fact]
    public void Create_FirstNameTooLong_ReturnsFirstNameTooLong()
    {
        var firstName = new string('a', PersonName.MaxFirstName + 1);

        var result = PersonName.Create(firstName, lastName: "Иванов", middleName: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("person_name.first_name.too_long");
    }

    /// <summary>
    /// LastName / MiddleName длиннее MaxLength — те же TooLong-ошибки
    /// с конкретными кодами.
    /// </summary>
    [Fact]
    public void Create_LastNameTooLong_ReturnsLastNameTooLong()
    {
        var lastName = new string('b', PersonName.MaxLastName + 1);

        var result = PersonName.Create(firstName: "Иван", lastName: lastName, middleName: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("person_name.last_name.too_long");
    }

    [Fact]
    public void Create_MiddleNameTooLong_ReturnsMiddleNameTooLong()
    {
        var middleName = new string('c', PersonName.MaxMiddleName + 1);

        var result = PersonName.Create(firstName: "Иван", lastName: "Иванов", middleName: middleName);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("person_name.middle_name.too_long");
    }

    /// <summary>
    /// FullName собирает строку формата "Фамилия Имя Отчество"
    /// (русская традиция). Покрываем оба случая: с MiddleName и без —
    /// без отчества лишний пробел не появляется.
    /// </summary>
    [Fact]
    public void FullName_AllPartsPresent_BuildsLastFirstMiddle()
    {
        var name = PersonName.Create("Иван", "Иванов", "Иванович").Value;

        name.FullName.Should().Be("Иванов Иван Иванович");
    }

    [Fact]
    public void FullName_WithoutMiddleName_BuildsLastFirst()
    {
        var name = PersonName.Create("Иван", "Иванов", middleName: null).Value;

        // Никаких лишних пробелов или висячих null'ов.
        name.FullName.Should().Be("Иванов Иван");
    }

    /// <summary>
    /// Equality: одинаковые имена дают равные VO. Trim применяется
    /// к каждому полю, поэтому "Иван " и "Иван" — это один и тот же VO.
    /// </summary>
    [Fact]
    public void Equality_SameNamesAfterTrim_AreEqual()
    {
        var a = PersonName.Create("Иван", "Иванов", "Иванович").Value;
        var b = PersonName.Create("Иван  ", "  Иванов", "Иванович  ").Value;

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    /// <summary>
    /// MiddleName из whitespace-only нормализуется до null
    /// (см. NormalizeOptional). Это значит, что VO с MiddleName="   "
    /// equals VO с MiddleName=null — иначе ломается уникальность
    /// SearchKey (одно и то же лицо считалось бы разными).
    /// </summary>
    [Fact]
    public void Equality_WhitespaceMiddleNameEqualsNullMiddleName()
    {
        var withNull = PersonName.Create("Иван", "Иванов", middleName: null).Value;
        var withWhitespace = PersonName.Create("Иван", "Иванов", middleName: "   ").Value;

        withNull.Should().Be(withWhitespace);
    }
}
