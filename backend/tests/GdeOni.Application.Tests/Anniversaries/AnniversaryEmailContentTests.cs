using GdeOni.Application.Anniversaries;

namespace GdeOni.Application.Tests.Anniversaries;

/// <summary>
/// D37. Формулировки тем и тел писем о годовщинах.
/// </summary>
public sealed class AnniversaryEmailContentTests
{
    [Fact]
    public void Build_Death_HasMemorialSubjectAndBody()
    {
        var message = AnniversaryEmailContent.Build(
            recipientEmail: "user@example.com",
            recipientName: "Иван",
            kind: AnniversaryKind.Death,
            deceasedFullName: "Петров Пётр Петрович",
            yearsSince: 5,
            appName: "Где Они",
            appUrl: "https://gdeoni.ru");

        message.ToEmail.Should().Be("user@example.com");
        message.Subject.Should().Contain("День памяти");
        message.Subject.Should().Contain("Петров Пётр Петрович");
        message.TextBody.Should().Contain("Здравствуйте, Иван!");
        message.TextBody.Should().Contain("со дня смерти");
        // 5 лет — форма «лет».
        message.TextBody.Should().Contain("5 лет");
        message.HtmlBody.Should().NotBeNullOrEmpty();
        message.HtmlBody.Should().Contain("https://gdeoni.ru");
    }

    [Fact]
    public void Build_Birth_HasBirthdaySubjectAndYearsWord()
    {
        var message = AnniversaryEmailContent.Build(
            recipientEmail: "user@example.com",
            recipientName: null,
            kind: AnniversaryKind.Birth,
            deceasedFullName: "Сидоров Сидор",
            yearsSince: 1,
            appName: "Где Они",
            appUrl: null);

        message.Subject.Should().Contain("День рождения");
        message.TextBody.Should().Contain("Здравствуйте!");
        message.TextBody.Should().Contain("исполнилось бы");
        // 1 год — форма «год».
        message.TextBody.Should().Contain("1 год");
        // Без appUrl ссылки в тексте нет.
        message.TextBody.Should().NotContain("http");
    }

    [Fact]
    public void Build_EncodesHtmlInName()
    {
        var message = AnniversaryEmailContent.Build(
            recipientEmail: "user@example.com",
            recipientName: "<b>hax</b>",
            kind: AnniversaryKind.Death,
            deceasedFullName: "A & B",
            yearsSince: 2,
            appName: "Где Они",
            appUrl: null);

        // В HTML имя/ФИО экранированы — нет «сырых» тегов из пользовательских данных.
        message.HtmlBody.Should().Contain("A &amp; B");
        message.HtmlBody.Should().Contain("&lt;b&gt;hax&lt;/b&gt;");
    }
}
