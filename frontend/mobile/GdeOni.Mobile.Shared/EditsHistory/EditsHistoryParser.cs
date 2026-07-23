using System.Text.Json;

namespace GdeOni.Mobile.Shared.EditsHistory;

/// <summary>
/// F17.9 / D-edits-history. Парсинг payload истории правок карточки
/// умершего. Payload — JSON-словарь {"FieldName": {"old": ..., "new": ...}, ...}.
/// Вытащено из mobile EditsHistoryViewModel/AllEditsHistoryViewModel
/// для шеринга с вебом и покрытия unit-тестами.
/// </summary>
public static class EditsHistoryParser
{
    public sealed record ChangePair(string? Old, string? New);
    public sealed record ChangeRow(string FieldLabel, string OldValue, string NewValue);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static string? ExtractPreviousAuthorEmail(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, ChangePair>>(json, JsonOptions);
            return dict?.TryGetValue("PreviousAuthor", out var pair) == true ? pair.Old : null;
        }
        catch { return null; }
    }

    public static IReadOnlyList<ChangeRow> ParseChanges(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<ChangeRow>();
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, ChangePair>>(json, JsonOptions);
            if (dict is null) return Array.Empty<ChangeRow>();
            return dict.Select(kv => new ChangeRow(
                FieldDisplay(kv.Key),
                kv.Value.Old ?? "—",
                kv.Value.New ?? "—")).ToList();
        }
        catch
        {
            return new[] { new ChangeRow("(diff)", "", json) };
        }
    }

    public static string FieldDisplay(string field) => field switch
    {
        "FirstName" => "Имя",
        "LastName" => "Фамилия",
        "MiddleName" => "Отчество",
        "BirthDate" => "Дата рождения",
        "DeathDate" => "Дата смерти",
        "ShortDescription" => "Краткое описание",
        "Biography" => "Биография",
        "Epitaph" => "Эпитафия",
        "Religion" => "Религия",
        "Source" => "Источник",
        "IsMilitaryService" => "Военная служба",
        "AdditionalInfo" => "Доп. информация",
        "Latitude" => "Широта",
        "Longitude" => "Долгота",
        "AccuracyMeters" => "Точность, м",
        "Country" => "Страна",
        "City" => "Город",
        "CemeteryName" => "Кладбище",
        "PlotNumber" => "Участок",
        "GraveNumber" => "Номер могилы",
        "PreviousAuthor" => "Удалённый автор (email)",
        _ => field,
    };
}
