using GdeOni.Application.Legal;
using GdeOni.Application.Legal.Queries.GetLegalDocument.Model;
using GdeOni.Application.Legal.Queries.GetLegalDocument.UseCase;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Tests.Legal;

/// <summary>
/// D19. Тесты <see cref="GetLegalDocumentUseCase"/>: возвращает
/// корректную пару URL+version для PrivacyPolicy / TermsOfUse.
/// </summary>
public sealed class GetLegalDocumentUseCaseTests
{
    [Fact]
    public async Task Execute_PrivacyPolicy_ReturnsCurrentVersionAndUrl()
    {
        var options = Options.Create(new LegalOptions
        {
            CurrentPrivacyPolicyVersion = 3,
            PrivacyPolicyUrl = "https://example/privacy",
        });
        var useCase = new GetLegalDocumentUseCase(options);

        var result = await useCase.Execute(
            new GetLegalDocumentQuery(LegalDocumentKey.PrivacyPolicy),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DocumentKey.Should().Be("privacy_policy");
        result.Value.Version.Should().Be(3);
        result.Value.Url.Should().Be("https://example/privacy");
        result.Value.BodyMarkdown.Should().BeNull();
    }

    [Fact]
    public async Task Execute_TermsOfUse_ReturnsCurrentVersionAndUrl()
    {
        var options = Options.Create(new LegalOptions
        {
            CurrentTermsVersion = 5,
            TermsUrl = "https://example/terms",
        });
        var useCase = new GetLegalDocumentUseCase(options);

        var result = await useCase.Execute(
            new GetLegalDocumentQuery(LegalDocumentKey.TermsOfUse),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DocumentKey.Should().Be("terms_of_use");
        result.Value.Version.Should().Be(5);
        result.Value.Url.Should().Be("https://example/terms");
    }
}
