/**
 * F14. Одноразовый promise-обёртка над navigator.geolocation, без
 * React state — для использования внутри mutationFn / await-цепочек.
 * Дублирует трёхступенчатую стратегию из useGeolocation (F5), чтобы
 * mutation мог работать на медленном соединении / без GPS-чипа.
 */
// Короче, чем useGeolocation (F5): тут юзер ждёт открытия карты в
// новой вкладке, 55+ секунд тишины пугает. В России Chrome/Yandex
// часто не могут добраться до Google Geolocation Service — лучше
// быстро failить и предложить fallback (для F14.1 — Yandex без origin,
// для F14.2 — маршрут от первой выбранной точки).
const ATTEMPTS: PositionOptions[] = [
  { enableHighAccuracy: true, timeout: 5_000, maximumAge: 0 },
  { enableHighAccuracy: false, timeout: 5_000, maximumAge: 60_000 },
  { enableHighAccuracy: false, timeout: 10_000, maximumAge: Infinity },
];

export type GeoCoords = {
  latitude: number;
  longitude: number;
  accuracyMeters: number;
};

export async function requestGeolocationOnce(): Promise<GeoCoords> {
  if (!('geolocation' in navigator)) {
    throw new Error('Браузер не поддерживает геолокацию.');
  }

  let lastError: GeolocationPositionError | null = null;
  for (const opts of ATTEMPTS) {
    try {
      const pos = await new Promise<GeolocationPosition>((resolve, reject) => {
        navigator.geolocation.getCurrentPosition(resolve, reject, opts);
      });
      return {
        latitude: pos.coords.latitude,
        longitude: pos.coords.longitude,
        accuracyMeters: pos.coords.accuracy,
      };
    } catch (e) {
      lastError = e as GeolocationPositionError;
      if (lastError.code === lastError.PERMISSION_DENIED) break;
    }
  }

  if (lastError?.code === lastError?.PERMISSION_DENIED) {
    throw new Error(
      'Доступ к местоположению запрещён. Откройте замочек в адресной строке и разрешите геолокацию.',
    );
  }
  if (lastError?.code === lastError?.TIMEOUT) {
    throw new Error(
      'Геолокация не отвечает. Это может быть из-за VPN или ограничений провайдера. Попробуйте позже.',
    );
  }
  throw new Error(
    'Не удалось определить местоположение. Проверьте, что геолокация устройства включена.',
  );
}
