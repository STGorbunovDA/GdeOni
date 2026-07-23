namespace GdeOni.Mobile.ViewModels.Support;

/// <summary>
/// D34. Re-export shared-парсера маркера "ID карточки: {guid}".
/// Логика живёт в GdeOni.Mobile.Shared.Support — общая с вебом
/// и покрытая unit-тестами в GdeOni.Mobile.Tests.
/// </summary>
internal static class SupportDeceasedRefParser
{
    public static Guid? TryExtract(string? description) =>
        Shared.Support.SupportDeceasedRefParser.TryExtract(description);
}
