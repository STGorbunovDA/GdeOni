using Microsoft.AspNetCore.Authorization;

namespace GdeOni.API.Authorization;

/// <summary>
/// D16. Requirement-маркер для политики "пользователь имеет активную
/// подписку (или Trial, или Admin-роль, или коммерциализация выключена)".
/// </summary>
public sealed class ActiveSubscriptionRequirement : IAuthorizationRequirement;
