#if ANDROID
using Android.Content;
using Android.Net;
#endif

namespace GdeOni.Mobile.Services.Network;

public sealed class NetworkInfoService : INetworkInfoService
{
    public bool IsVpnActive()
    {
#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var cm = (ConnectivityManager?)context.GetSystemService(Context.ConnectivityService);
            if (cm is null)
                return false;

            // GetAllNetworks помечен deprecated на API 31+, но альтернатива
            // (NetworkCallback) — асинхронная подписка, для разовой проверки
            // избыточна. Сам метод работает и на новых API.
#pragma warning disable CA1422
            var networks = cm.GetAllNetworks();
#pragma warning restore CA1422
            if (networks is null)
                return false;

            foreach (var network in networks)
            {
                var caps = cm.GetNetworkCapabilities(network);
                if (caps is null)
                    continue;

                if (caps.HasTransport(TransportType.Vpn))
                    return true;
            }
        }
        catch
        {
            // Любая ошибка определения VPN не должна ломать flow получения
            // координат: возвращаем false и пускай юзер пробует геолокацию.
        }
#endif
        return false;
    }
}
