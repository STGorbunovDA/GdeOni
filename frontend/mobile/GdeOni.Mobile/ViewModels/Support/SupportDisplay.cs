namespace GdeOni.Mobile.ViewModels.Support;

/// <summary>
/// D25 mobile. Локализованные подписи и цвета для enum'ов
/// status/severity/kind/source. Один источник истины для всех
/// support-страниц.
/// </summary>
internal static class SupportDisplay
{
    public static (string Display, string Color) Status(string code) => code switch
    {
        "Open" => ("Открыто", "#F9A825"),         // янтарный — ждёт реакции
        "InProgress" => ("В работе", "#1976D2"),   // синий — активно
        "Resolved" => ("Решено", "#2E7D32"),       // зелёный — завершено
        _ => (code, "#000000"),
    };

    public static (string Display, string Color) Severity(string code) => code switch
    {
        "Normal" => ("Обычно", "#7F8C8D"),  // серый — фоновый
        "Urgent" => ("Срочно", "#C0392B"),  // красный — внимание
        _ => (code, "#000000"),
    };

    public static string Kind(string code) => code switch
    {
        "Payment" => "Платёж",
        "Bug" => "Ошибка / Баг",
        "Complaint" => "Жалоба",
        "Question" => "Вопрос",
        "Other" => "Другое",
        _ => code,
    };

    public static string Source(string code) => code switch
    {
        "Manual" => "От пользователя",
        "Auto" => "Авто-инцидент",
        _ => code,
    };
}
