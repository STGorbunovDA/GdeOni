using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Legal.Queries.GetLegalDocument.Model;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Legal.Queries.GetLegalDocument.UseCase;

/// <summary>
/// D19. Отдаёт метаданные юридического документа — текущую версию и
/// публичный URL — клиенту. Body документа в этой итерации не
/// возвращаем: клиент сам ходит по URL за markdown'ом (статика
/// хостится на gdeoni.ru/legal/...). Если когда-то понадобится
/// "встроенный текст в API" — расширим <see cref="LegalDocumentResponse.BodyMarkdown"/>
/// через отдельный source-сервис в Infrastructure.
/// </summary>
public sealed class GetLegalDocumentUseCase(
    IOptions<LegalOptions> legalOptions)
    : IGetLegalDocumentUseCase
{
    public Task<Result<LegalDocumentResponse, Error>> Execute(
        GetLegalDocumentQuery query,
        CancellationToken cancellationToken)
    {
        var legal = legalOptions.Value;
        return Task.FromResult(query.DocumentKey switch
        {
            LegalDocumentKey.PrivacyPolicy => Result.Success<LegalDocumentResponse, Error>(
                new LegalDocumentResponse(
                    "privacy_policy",
                    legal.CurrentPrivacyPolicyVersion,
                    legal.PrivacyPolicyUrl,
                    BodyMarkdown: null)),
            LegalDocumentKey.TermsOfUse => Result.Success<LegalDocumentResponse, Error>(
                new LegalDocumentResponse(
                    "terms_of_use",
                    legal.CurrentTermsVersion,
                    legal.TermsUrl,
                    BodyMarkdown: null)),
            _ => Errors.Legal.DocumentNotFound(query.DocumentKey.ToString()),
        });
    }
}
