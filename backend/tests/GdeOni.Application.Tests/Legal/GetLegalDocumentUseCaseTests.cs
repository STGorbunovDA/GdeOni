using GdeOni.Application.Abstractions.Legal;
using GdeOni.Application.Legal;
using GdeOni.Application.Legal.Queries.GetLegalDocument.Model;
using GdeOni.Application.Legal.Queries.GetLegalDocument.UseCase;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Tests.Legal;

/// <summary>
/// D19 / D19.9. Тесты <see cref="GetLegalDocumentUseCase"/>: возвращает
/// корректную тройку URL + version + текст для PrivacyPolicy / TermsOfUse.
/// </summary>
public sealed class GetLegalDocumentUseCaseTests
{
    /// <summary>
    /// Заглушка источника текстов: реальная реализация читает файлы из
    /// {ContentRoot}/Legal, что в unit-тесте не нужно.
    /// </summary>
    private sealed class StubDocumentSource(string? markdown = null) : ILegalDocumentSource
    {
        public string? GetMarkdown(LegalDocumentKey key) => markdown;

        public int? GetDeclaredVersion(LegalDocumentKey key) => null;
    }

    [Fact]
    public async Task Execute_PrivacyPolicy_ReturnsCurrentVersionUrlAndBody()
    {
        var options = Options.Create(new LegalOptions
        {
            CurrentPrivacyPolicyVersion = 3,
            PrivacyPolicyUrl = "https://example/privacy",
        });
        var useCase = new GetLegalDocumentUseCase(
            options,
            new StubDocumentSource("# Политика\n\nРедакция 3."));

        var result = await useCase.Execute(
            new GetLegalDocumentQuery(LegalDocumentKey.PrivacyPolicy),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DocumentKey.Should().Be("privacy_policy");
        result.Value.Version.Should().Be(3);
        result.Value.Url.Should().Be("https://example/privacy");
        result.Value.BodyMarkdown.Should().Contain("Редакция 3");
    }

    [Fact]
    public async Task Execute_TermsOfUse_ReturnsCurrentVersionAndUrl()
    {
        var options = Options.Create(new LegalOptions
        {
            CurrentTermsVersion = 5,
            TermsUrl = "https://example/terms",
        });
        var useCase = new GetLegalDocumentUseCase(
            options,
            new StubDocumentSource("# Соглашение"));

        var result = await useCase.Execute(
            new GetLegalDocumentQuery(LegalDocumentKey.TermsOfUse),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DocumentKey.Should().Be("terms_of_use");
        result.Value.Version.Should().Be(5);
        result.Value.Url.Should().Be("https://example/terms");
    }

    /// <summary>
    /// Файл текста может отсутствовать (например, кто-то выкинул его из
    /// сборки) — эндпоинт всё равно обязан отдать версию и URL, чтобы
    /// клиент показал документ по ссылке.
    /// </summary>
    [Fact]
    public async Task Execute_WhenMarkdownMissing_StillReturnsVersionAndUrl()
    {
        var options = Options.Create(new LegalOptions());
        var useCase = new GetLegalDocumentUseCase(options, new StubDocumentSource(null));

        var result = await useCase.Execute(
            new GetLegalDocumentQuery(LegalDocumentKey.PrivacyPolicy),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BodyMarkdown.Should().BeNull();
        result.Value.Url.Should().NotBeNullOrWhiteSpace();
    }
}
