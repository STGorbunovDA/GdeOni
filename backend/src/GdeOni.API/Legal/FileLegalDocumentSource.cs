using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using GdeOni.Application.Abstractions.Legal;
using GdeOni.Application.Legal.Queries.GetLegalDocument.Model;

namespace GdeOni.API.Legal;

/// <summary>
/// D19.9. Читает тексты юр-документов из файлов, которые csproj
/// копирует в <c>{ContentRoot}/Legal/</c> из <c>backend/docs/legal/</c>.
/// Реализация лежит в API, а не в Infrastructure, потому что зависит от
/// ContentRoot (IHostEnvironment) — это хостовая деталь, не персистенс.
///
/// Singleton + кеш: файл читается один раз при первом обращении.
/// Документ меняется раз в год вместе с деплоем, hot-reload не нужен.
/// </summary>
public sealed partial class FileLegalDocumentSource(
    IHostEnvironment environment,
    ILogger<FileLegalDocumentSource> logger) : ILegalDocumentSource
{
    private readonly ConcurrentDictionary<LegalDocumentKey, Document> _cache = new();

    private sealed record Document(string? Markdown, int? DeclaredVersion);

    /// <summary>Markdown-текст документа; null, если файла нет.</summary>
    public string? GetMarkdown(LegalDocumentKey key) => Load(key).Markdown;

    /// <summary>Номер редакции, объявленный в шапке самого документа.</summary>
    public int? GetDeclaredVersion(LegalDocumentKey key) => Load(key).DeclaredVersion;

    private Document Load(LegalDocumentKey key) =>
        _cache.GetOrAdd(key, k =>
        {
            var path = ResolvePath(FileNameOf(k));

            if (path is null)
            {
                // Не бросаем: эндпоинт останется рабочим и отдаст версию +
                // публичный URL, клиент покажет текст по ссылке. Отсутствие
                // файла ловится fail-fast проверкой на старте
                // (LegalDocumentsStartupCheck), сюда мы попадём только если
                // её отключили.
                logger.LogError("Файл юр-документа {File} не найден.", FileNameOf(k));
                return new Document(null, null);
            }

            var markdown = File.ReadAllText(path);
            return new Document(markdown, ParseDeclaredVersion(markdown));
        });

    /// <summary>
    /// Ищем .md рядом со сборкой (BaseDirectory) — туда csproj кладёт
    /// файлы через CopyToOutputDirectory, и оттуда же их видят
    /// интеграционные тесты. ContentRoot — второй кандидат: при
    /// `dotnet run` он указывает на КАТАЛОГ ПРОЕКТА, где .md нет, зато в
    /// опубликованном виде (dotnet publish / Docker) оба пути совпадают.
    /// </summary>
    private string? ResolvePath(string fileName)
    {
        string[] roots = [AppContext.BaseDirectory, environment.ContentRootPath];

        return roots
            .Select(root => Path.Combine(root, "Legal", fileName))
            .FirstOrDefault(File.Exists);
    }

    private static string FileNameOf(LegalDocumentKey key) => key switch
    {
        LegalDocumentKey.PrivacyPolicy => "privacy-policy.md",
        LegalDocumentKey.TermsOfUse => "terms-of-use.md",
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Неизвестный юр-документ."),
    };

    /// <summary>
    /// Вытаскивает N из строки «Редакция N. Действует с ...» в шапке
    /// документа. Это единственное место, где номер версии живёт внутри
    /// самого текста — чтобы юзер видел, что именно он принимает.
    /// </summary>
    private static int? ParseDeclaredVersion(string markdown)
    {
        var match = RevisionRegex().Match(markdown);
        return match.Success && int.TryParse(match.Groups[1].Value, out var version)
            ? version
            : null;
    }

    [GeneratedRegex(@"^Редакция\s+(\d+)\b", RegexOptions.Multiline)]
    private static partial Regex RevisionRegex();
}
