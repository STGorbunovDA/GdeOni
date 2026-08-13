namespace GdeOni.Application.Users.Queries.GetAll.Model;

public sealed class GetAllUsersResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = null!;
    public string UserName { get; init; } = null!;

    /// <summary>
    /// Уникальный логин (вход по email или логину). Отдельная колонка в
    /// админ-списке: UserName — отображаемое имя и допускает тёзок.
    /// </summary>
    public string Login { get; init; } = null!;

    public string? FullName { get; init; }
    public string Role { get; init; } = null!;
    public DateTime RegisteredAtUtc { get; init; }
    public DateTime? LastLoginAtUtc { get; init; }
    public int TrackingCount { get; init; }

    /// <summary>
    /// F17.10. Признак блокировки — чтобы UI листинга мог отметить
    /// заблокированных красным/значком, не загружая детали.
    /// </summary>
    public bool IsBlocked { get; init; }
}