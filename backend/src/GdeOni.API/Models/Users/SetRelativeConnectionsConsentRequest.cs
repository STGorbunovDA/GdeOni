namespace GdeOni.API.Models.Users;

/// <summary>
/// Тело <c>PATCH /api/users/me/relative-connections</c>. Функция
/// «Родственники»: разрешить (true) или запретить (false) видимость
/// текущего пользователя как родственника и получение сообщений.
/// </summary>
public sealed class SetRelativeConnectionsConsentRequest
{
    /// <summary>Разрешить видимость как родственника и переписку.</summary>
    public bool Allow { get; set; }
}
