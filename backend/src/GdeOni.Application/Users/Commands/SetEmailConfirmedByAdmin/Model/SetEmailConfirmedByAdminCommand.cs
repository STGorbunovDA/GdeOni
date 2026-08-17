namespace GdeOni.Application.Users.Commands.SetEmailConfirmedByAdmin.Model;

/// <summary>
/// Админ вручную подтверждает адрес пользователя или снимает подтверждение.
/// <paramref name="Confirmed"/> = true — подтвердить, false — снять.
/// </summary>
public sealed record SetEmailConfirmedByAdminCommand(Guid TargetUserId, bool Confirmed);
