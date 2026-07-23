import { useCallback, useState } from 'react';

/**
 * F5. Браузерная геолокация. Зеркало mobile GeolocationService.
 *
 * Особенности браузерной геолокации:
 *  - Permission prompt браузер показывает сам (юзер кликнул кнопку
 *    → браузер всплывает с разрешением).
 *  - На production обязателен HTTPS. На localhost работает по HTTP
 *    (Chrome делает исключение).
 *  - Если юзер permanently denied — второй request тоже сразу падает
 *    с PERMISSION_DENIED. UI подсказывает про 'замочек' в адресной
 *    строке.
 *  - В России Chrome/Yandex.Browser иногда долго ждут ответ от
 *    Google Location Service (geomobile.googleapis.com), который без
 *    VPN может отвечать медленно или зависать. Поэтому стратегия
 *    запроса даёт сразу несколько попыток с разной точностью.
 *
 * Стратегия (трёхступенчатая):
 *  1. High-accuracy с timeout 10s, maximumAge 0 — честный GPS-fix
 *     (получится на телефоне или ноуте с GPS-чипом).
 *  2. Low-accuracy с timeout 15s, maximumAge 60s — WiFi/IP-позиция
 *     или кэш минуты (на desktop без GPS).
 *  3. Low-accuracy с timeout 30s, maximumAge ∞ — берём любой кэш
 *     если есть, или ждём дольше (медленное соединение / VPN).
 *
 * PERMISSION_DENIED на любом шаге → сразу отдаём ошибку, фоллбэк
 * не помогает.
 *
 * Если все три шага упали — выдаём error.code='timeout' с подсказкой
 * "введите координаты вручную" (в F8/F15 рядом с GhostButton
 * 'Определить' будут поля ввода lat/lon, доступные всегда).
 */

export type GeoPosition = {
  latitude: number;
  longitude: number;
  accuracyMeters: number;
};

export type GeoErrorCode =
  | 'unsupported'
  | 'permission_denied'
  | 'position_unavailable'
  | 'timeout';

export type GeoError = {
  code: GeoErrorCode;
  message: string;
};

export type GeoStatus = 'idle' | 'requesting' | 'success' | 'error';

export type UseGeolocationResult = {
  status: GeoStatus;
  position: GeoPosition | null;
  error: GeoError | null;
  request: () => void;
  reset: () => void;
};

const ATTEMPTS: PositionOptions[] = [
  { enableHighAccuracy: true, timeout: 10_000, maximumAge: 0 },
  { enableHighAccuracy: false, timeout: 15_000, maximumAge: 60_000 },
  { enableHighAccuracy: false, timeout: 30_000, maximumAge: Infinity },
];

function mapBrowserError(err: GeolocationPositionError): GeoError {
  switch (err.code) {
    case err.PERMISSION_DENIED:
      return {
        code: 'permission_denied',
        message:
          'Доступ к местоположению запрещён. Откройте замочек в адресной строке и разрешите геолокацию для этого сайта.',
      };
    case err.POSITION_UNAVAILABLE:
      return {
        code: 'position_unavailable',
        message:
          'Не удалось определить местоположение. Проверьте, что геолокация устройства включена.',
      };
    case err.TIMEOUT:
      return {
        code: 'timeout',
        message:
          'Геолокация не отвечает. Это может быть из-за VPN, ограничений провайдера или плохого сигнала. Введите координаты вручную или попробуйте позже.',
      };
    default:
      return { code: 'position_unavailable', message: err.message };
  }
}

/**
 * Промис-обёртка над getCurrentPosition. Reject'ит только при ошибке.
 * Нужно чтобы можно было привязать try/await в цепочке попыток.
 */
function getPositionAsync(options: PositionOptions): Promise<GeolocationPosition> {
  return new Promise((resolve, reject) => {
    navigator.geolocation.getCurrentPosition(resolve, reject, options);
  });
}

export function useGeolocation(): UseGeolocationResult {
  const [status, setStatus] = useState<GeoStatus>('idle');
  const [position, setPosition] = useState<GeoPosition | null>(null);
  const [error, setError] = useState<GeoError | null>(null);

  const request = useCallback(async () => {
    if (status === 'requesting') return;

    if (!('geolocation' in navigator)) {
      setError({
        code: 'unsupported',
        message: 'Браузер не поддерживает геолокацию.',
      });
      setStatus('error');
      return;
    }

    setStatus('requesting');
    setError(null);

    let lastError: GeolocationPositionError | null = null;

    for (let i = 0; i < ATTEMPTS.length; i++) {
      try {
        const pos = await getPositionAsync(ATTEMPTS[i]);
        setPosition({
          latitude: pos.coords.latitude,
          longitude: pos.coords.longitude,
          accuracyMeters: pos.coords.accuracy,
        });
        setStatus('success');
        return;
      } catch (e) {
        const err = e as GeolocationPositionError;
        lastError = err;
        console.warn(
          `[useGeolocation] attempt ${i + 1}/${ATTEMPTS.length} failed: code=${err.code} message="${err.message}"`,
        );
        // PERMISSION_DENIED не лечится фоллбэком — выходим сразу.
        if (err.code === err.PERMISSION_DENIED) {
          break;
        }
      }
    }

    setError(
      lastError
        ? mapBrowserError(lastError)
        : { code: 'position_unavailable', message: 'Неизвестная ошибка.' },
    );
    setStatus('error');
  }, [status]);

  const reset = useCallback(() => {
    setStatus('idle');
    setPosition(null);
    setError(null);
  }, []);

  return { status, position, error, request, reset };
}
