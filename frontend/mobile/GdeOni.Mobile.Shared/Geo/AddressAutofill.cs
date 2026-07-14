namespace GdeOni.Mobile.Shared.Geo;

/// <summary>
/// D41. Слияние автоопределённого адреса с тем, что уже введено.
///
/// Зеркало web-утилиты <c>mergeAutofilled</c> (frontend/web/src/utils/
/// addressAutofill.ts) — правило должно быть одинаковым на обоих клиентах,
/// иначе одно и то же действие даёт разный результат.
///
/// Задача: подставить город по координатам, но не затереть ручной ввод.
/// Правило: перезаписываем, только если поле пустое ИЛИ в нём ровно то, что
/// мы сами подставили в прошлый раз. Как только человек его отредактировал —
/// поле принадлежит ему.
/// </summary>
public static class AddressAutofill
{
    public static string Merge(string current, string previousAuto, string? incoming)
    {
        // Геокодер ничего не дал — пустое поле лучше выдумки.
        if (string.IsNullOrWhiteSpace(incoming))
            return current;

        var untouched =
            string.IsNullOrWhiteSpace(current)
            || string.Equals(current.Trim(), previousAuto.Trim(), StringComparison.Ordinal);

        return untouched ? incoming : current;
    }
}
