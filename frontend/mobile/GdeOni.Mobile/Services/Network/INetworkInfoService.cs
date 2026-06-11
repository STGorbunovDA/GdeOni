namespace GdeOni.Mobile.Services.Network;

public interface INetworkInfoService
{
    /// <summary>
    /// Возвращает true, если на устройстве сейчас активно хотя бы одно
    /// VPN-соединение. Под VPN GPS на Android физически работает, но
    /// часть пользователей считает "не работает локация" симптомом VPN —
    /// поэтому показываем подсказку, чтобы они отключили VPN, если что.
    /// </summary>
    bool IsVpnActive();
}
