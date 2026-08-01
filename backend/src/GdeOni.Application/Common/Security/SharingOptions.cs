namespace GdeOni.Application.Common.Security;

/// <summary>
/// D46. Настройки функции «Поделиться подборкой». Биндится из секции
/// <c>Sharing</c> в appsettings.
/// </summary>
public sealed class SharingOptions
{
    public const string SectionName = "Sharing";

    /// <summary>
    /// Сколько живёт ссылка/QR. 24 часа по умолчанию (решение 2026-08-01):
    /// подборка передаётся «здесь и сейчас», старые QR жить вечно не должны.
    /// </summary>
    public int LinkLifetimeHours { get; set; } = 24;
}
