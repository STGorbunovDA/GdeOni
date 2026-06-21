namespace GdeOni.API.Models.Users;

/// <summary>
/// Параметры пагинации для выборки персонального списка отслеживаемых
/// умерших текущего пользователя.
/// </summary>
public sealed class GetMyTrackedDeceasedRequest
{
    /// <summary>Номер страницы (от 1).</summary>
    public int Page { get; set; } = 1;
    /// <summary>Размер страницы.</summary>
    public int PageSize { get; set; } = 20;
}
