namespace GdeOni.API.Models.Users;

/// <summary>
/// Ручное управление подтверждением email со стороны админа.
/// </summary>
public sealed class SetEmailConfirmedRequest
{
    /// <summary>true — подтвердить адрес, false — снять подтверждение.</summary>
    public bool Confirmed { get; set; }
}
