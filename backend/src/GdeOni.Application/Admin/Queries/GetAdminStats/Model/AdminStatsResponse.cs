namespace GdeOni.Application.Admin.Queries.GetAdminStats.Model;

/// <summary>
/// F38. Справка для админа: сколько в системе людей, карточек, контента,
/// денег. Чисто информационный снимок — никаких действий с ним не
/// делается, поэтому это плоские счётчики, а не полноценная аналитика.
/// </summary>
public sealed record AdminStatsResponse(
    AdminUsersStats Users,
    AdminDeceasedStats Deceased,
    AdminContentStats Content,
    AdminSupportStats Support,
    AdminPaymentsStats Payments,
    DateTime GeneratedAtUtc);

/// <summary>
/// Пользователи и их доступ.
/// ActiveLast30Days — заходили за последние 30 дней (по LastLoginAtUtc).
/// Admins — SuperAdmin + Admin.
/// WithActiveSubscription — оплачена и ещё не истекла.
/// WithComplimentaryAccess — бесплатный доступ, выданный админом.
/// </summary>
public sealed record AdminUsersStats(
    int Total,
    int NewLast7Days,
    int NewLast30Days,
    int ActiveLast30Days,
    int Admins,
    int Blocked,
    int WithActiveSubscription,
    int OnTrial,
    int WithComplimentaryAccess);

/// <summary>
/// Карточки умерших.
/// WithCoordinates — есть координаты захоронения; по ним работают
/// маршрут и «найти рядом», без них карточка наполовину бесполезна.
/// WithMainPhoto — задано главное фото, иначе в поиске карточка без превью.
/// TrackedRecords — число записей отслеживания (не людей и не карточек).
/// </summary>
public sealed record AdminDeceasedStats(
    int Total,
    int NewLast30Days,
    int Verified,
    int WithCoordinates,
    int WithMainPhoto,
    int TrackedRecords);

/// <summary>Пользовательский контент вокруг карточек.</summary>
public sealed record AdminContentStats(
    int Photos,
    int GravePhotos,
    int Documents,
    int Memories,
    int Edits);

/// <summary>
/// Обращения в поддержку. Open — Open + InProgress, то есть всё, что
/// ждёт админа.
/// </summary>
public sealed record AdminSupportStats(
    int Total,
    int Open,
    int Resolved);

/// <summary>Платежи. Считаем только успешные — остальное шум.</summary>
public sealed record AdminPaymentsStats(
    int SucceededCount,
    decimal TotalRub,
    decimal Last30DaysRub);
