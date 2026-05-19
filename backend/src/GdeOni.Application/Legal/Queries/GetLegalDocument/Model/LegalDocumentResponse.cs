namespace GdeOni.Application.Legal.Queries.GetLegalDocument.Model;

/// <summary>
/// D19. Информация о юридическом документе для отображения клиентом.
/// <c>BodyMarkdown</c> — текст в Markdown, если хранится на сервере;
/// иначе клиент берёт текст по <see cref="Url"/>.
/// </summary>
public sealed record LegalDocumentResponse(
    string DocumentKey,
    int Version,
    string Url,
    string? BodyMarkdown);
