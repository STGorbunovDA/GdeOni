namespace GdeOni.Application.Legal.Queries.GetLegalDocument.Model;

/// <summary>
/// D19. Запрос юридического документа. Различение какого именно
/// документа отдать — через <see cref="DocumentKey"/> (privacy_policy
/// или terms_of_use).
/// </summary>
public sealed record GetLegalDocumentQuery(LegalDocumentKey DocumentKey);

public enum LegalDocumentKey
{
    PrivacyPolicy = 1,
    TermsOfUse = 2,
}
