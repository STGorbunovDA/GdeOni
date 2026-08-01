namespace GdeOni.Application.Sharing.Queries.GetShareBundle.Model;

/// <summary>
/// D46. Раскрытая подборка: строки карточек (несуществующие уже
/// отфильтрованы) и срок действия ссылки.
/// </summary>
public sealed record GetShareBundleResponse(
    IReadOnlyList<ShareBundleItemResponse> Items,
    DateTime ExpiresAtUtc);
