using GdeOni.Application.Legal.Queries.GetLegalDocument.Model;

namespace GdeOni.Application.Abstractions.Legal;

/// <summary>
/// D19.9. Источник текста юридических документов. Канонические тексты
/// лежат в <c>backend/docs/legal/*.md</c> и едут вместе с API — оба
/// клиента (web и mobile) берут их через <c>GET /api/legal/*</c>, а не
/// хранят свою копию. Раньше текст жил в бандле web, а версия — в
/// appsettings бэка: две правки в разных местах, которые обязаны
/// совпадать, но ничем не были связаны.
///
/// Реализация читает файлы один раз и держит в памяти — документ
/// меняется раз в год, при рестарте приложения.
/// </summary>
public interface ILegalDocumentSource
{
    /// <summary>
    /// Markdown-текст документа. Null, если файл не найден — клиент в
    /// этом случае откатится на публичный URL из <c>LegalOptions</c>.
    /// </summary>
    string? GetMarkdown(LegalDocumentKey key);

    /// <summary>
    /// Номер редакции, объявленный в самом документе (строка
    /// «Редакция N.»). Нужен для fail-fast сверки с
    /// <c>LegalOptions.Current*Version</c> на старте: расхождение
    /// означает, что юзеру покажут один текст, а согласие запишут на
    /// другую версию. Null, если строку не удалось разобрать.
    /// </summary>
    int? GetDeclaredVersion(LegalDocumentKey key);
}
