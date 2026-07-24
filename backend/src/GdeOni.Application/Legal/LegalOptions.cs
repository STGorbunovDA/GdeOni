namespace GdeOni.Application.Legal;

/// <summary>
/// D19. Версии и пути к юридическим документам (Privacy Policy / Terms
/// of Use). Биндятся из секции <c>Legal</c> в appsettings.
///
/// Версия = монотонно растущий int. Когда юристы пересмотрели документ
/// и в нём появились существенные изменения — поднимаем версию на 1.
/// Это форсирует UI (через <c>User.HasOutdatedLegalAcceptance</c>)
/// показать пользователю модалку "Документы обновились, прочитайте
/// заново и подтвердите".
/// </summary>
public sealed class LegalOptions
{
    public const string SectionName = "Legal";

    /// <summary>
    /// Текущая версия Privacy Policy. Используется при регистрации
    /// (записывается в User.PrivacyPolicyVersion) и при AcceptLegal
    /// (валидируется, что клиент не прислал устаревшую версию).
    ///
    /// D19.9: дефолт обязан совпадать со строкой «Редакция N.» в
    /// backend/docs/legal/privacy-policy.md — иначе API не стартует
    /// (LegalDocumentsStartupCheck). Меняешь текст — правь оба места.
    /// </summary>
    public int CurrentPrivacyPolicyVersion { get; set; } = 3;

    /// <summary>
    /// Текущая версия Terms of Use.
    /// </summary>
    public int CurrentTermsVersion { get; set; } = 2;

    /// <summary>
    /// Публичный URL текста Privacy Policy. Mobile / web ходят сюда
    /// напрямую (или через <c>/api/legal/privacy-policy</c> который
    /// просто отдаёт этот URL + версию для удобства).
    /// </summary>
    public string PrivacyPolicyUrl { get; set; } = "https://gdeoni.ru/legal/privacy";

    /// <summary>
    /// Публичный URL текста Terms of Use.
    /// </summary>
    public string TermsUrl { get; set; } = "https://gdeoni.ru/legal/terms";
}
