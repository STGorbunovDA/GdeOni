namespace GdeOni.API.Models.App;

/// <summary>
/// F40. Ответ <c>GET /api/app/stats</c> — публичные «живые» счётчики для
/// стартовой страницы: сколько всего зарегистрировано пользователей, сколько
/// заведено карточек памяти и в скольких разных городах есть захоронения.
/// AllowAnonymous (лендинг видит гость), результат кешируется на бэке.
/// </summary>
public sealed record AppStatsResponse(int UsersCount, int DeceasedCount, int CitiesCount);
