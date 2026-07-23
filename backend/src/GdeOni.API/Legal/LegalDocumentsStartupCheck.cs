using GdeOni.Application.Abstractions.Legal;
using GdeOni.Application.Legal;
using GdeOni.Application.Legal.Queries.GetLegalDocument.Model;
using Microsoft.Extensions.Options;

namespace GdeOni.API.Legal;

/// <summary>
/// D19.9. Fail-fast на старте: текст документа и его версия обязаны
/// совпадать.
///
/// Версия живёт в <c>LegalOptions</c> (по ней <c>HasOutdatedLegalAcceptance</c>
/// решает, форсить ли переподтверждение), а текст — в
/// <c>backend/docs/legal/*.md</c>. Если поднять версию в конфиге и забыть
/// обновить текст (или наоборот), пользователю покажут одну редакцию, а
/// согласие запишут на другую — юридически это хуже, чем не спрашивать
/// вовсе. Поэтому расхождение = падение на старте, как с миграциями.
/// </summary>
public static class LegalDocumentsStartupCheck
{
    /// <summary>
    /// Бросает <see cref="InvalidOperationException"/>, если текста
    /// документа нет или объявленная в нём редакция не совпадает с
    /// версией из секции <c>Legal</c>.
    /// </summary>
    public static void EnsureLegalDocumentsMatchVersions(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var source = scope.ServiceProvider.GetRequiredService<ILegalDocumentSource>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<LegalOptions>>().Value;

        Check(source, LegalDocumentKey.PrivacyPolicy, options.CurrentPrivacyPolicyVersion);
        Check(source, LegalDocumentKey.TermsOfUse, options.CurrentTermsVersion);
    }

    private static void Check(
        ILegalDocumentSource source,
        LegalDocumentKey key,
        int configuredVersion)
    {
        if (string.IsNullOrWhiteSpace(source.GetMarkdown(key)))
            throw new InvalidOperationException(
                $"Текст юр-документа {key} не найден. Ожидается файл в " +
                "backend/docs/legal (копируется в {ContentRoot}/Legal при сборке).");

        var declared = source.GetDeclaredVersion(key);

        if (declared is null)
            throw new InvalidOperationException(
                $"В тексте юр-документа {key} нет строки «Редакция N.» — " +
                "невозможно сверить версию с конфигурацией Legal.");

        if (declared.Value != configuredVersion)
            throw new InvalidOperationException(
                $"Расхождение версий юр-документа {key}: в тексте «Редакция {declared.Value}», " +
                $"в конфигурации Legal — {configuredVersion}. Поднимая версию, обнови оба места.");
    }
}
