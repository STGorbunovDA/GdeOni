namespace GdeOni.Infrastructure.Security;

public sealed class BCryptOptions
{
    public const string SectionName = "BCrypt";

    /// <summary>
    /// BCrypt work-factor (cost). 10 — дефолт BCrypt.Net (быстрый
    /// прогон тестов и dev-сценариев). В проде поднять до 12 через
    /// appsettings: "BCrypt": { "WorkFactor": 12 }. Каждый шаг
    /// удваивает время хеширования.
    /// </summary>
    public int WorkFactor { get; set; } = 10;
}
