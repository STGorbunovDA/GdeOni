using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.Users;

/// <summary>
/// Параметры выборки списка пользователей в админке: пагинация и
/// фильтры по роли, дате регистрации и активности.
/// </summary>
public sealed class GetAllUsersRequest
{
    /// <summary>Подстрочный поиск по email/UserName/FullName.</summary>
    public string? Search { get; set; }
    /// <summary>Фильтр по роли.</summary>
    public UserRole? Role { get; init; }
    /// <summary>Точная дата регистрации (UTC).</summary>
    public DateTime? RegisteredAtUtc { get; init; }
    /// <summary>Точная дата последнего входа (UTC).</summary>
    public DateTime? LastLoginAtUtc { get; init; }
    /// <summary>Номер страницы (от 1).</summary>
    public int Page { get; set; } = 1;
    /// <summary>Размер страницы.</summary>
    public int PageSize { get; set; } = 20;
    /// <summary>Нижняя граница периода регистрации в UTC (включительно).</summary>
    public DateTime? RegisteredFromUtc { get; init; }
    /// <summary>Верхняя граница периода регистрации в UTC (включительно).</summary>
    public DateTime? RegisteredToUtc { get; init; }
}
