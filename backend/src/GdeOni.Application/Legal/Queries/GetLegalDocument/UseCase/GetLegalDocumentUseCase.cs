using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Legal;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Legal.Queries.GetLegalDocument.Model;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Legal.Queries.GetLegalDocument.UseCase;

/// <summary>
/// D19 / D19.9. Отдаёт юридический документ: версию, публичный URL и сам
/// текст в Markdown.
///
/// Текст берётся из <see cref="ILegalDocumentSource"/> — канонические
/// файлы <c>backend/docs/legal/*.md</c>, которые едут вместе с API. Оба
/// клиента (web-страница /legal/*, mobile) рендерят один и тот же текст
/// из этого ответа и своей копии не хранят: раньше текст лежал в бандле
/// web, а версия — в appsettings бэка, и ничто не мешало им разъехаться.
///
/// Url остаётся в ответе как публичная ссылка «показать документ
/// человеку» (её же шлём в письмах и открываем из mobile-браузера).
/// </summary>
public sealed class GetLegalDocumentUseCase(
    IOptions<LegalOptions> legalOptions,
    ILegalDocumentSource documentSource)
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
                    documentSource.GetMarkdown(LegalDocumentKey.PrivacyPolicy))),
            LegalDocumentKey.TermsOfUse => Result.Success<LegalDocumentResponse, Error>(
                new LegalDocumentResponse(
                    "terms_of_use",
                    legal.CurrentTermsVersion,
                    legal.TermsUrl,
                    documentSource.GetMarkdown(LegalDocumentKey.TermsOfUse))),
            _ => Errors.Legal.DocumentNotFound(query.DocumentKey.ToString()),
        });
    }
}
