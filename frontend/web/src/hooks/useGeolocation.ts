import { useCallback, useState } from 'react';

/**
 * F5. Браузерная геолокация. Зеркало mobile GeolocationService.
 *
 * Особенности:
 *  - Permission prompt браузер показывает сам (юзер кликнул кнопку
 *    'Определить местоположение' → браузер всплывает с разрешением).
 *  - На production обязателен HTTPS — без него navigator.geolocation
 *    вызывает success/error callback'и, но reject'ит с PERMISSION_DENIED.
 *    На localhost работает по HTTP — Chrome делает исключение.
 *  - Если юзер permanently denied (отказал и поставил галочку
 *    'не спрашивать'), второй request тоже сразу падает с
 *    PERMISSION_DENIED. UI должен подсказать, как сбросить разрешение
 *    через 'замочек' в адресной строке.
 *
 * Не используем navigator.permissions.query — Safari её игнорирует,
 * а нам и так getCurrentPosition даёт правильный error.code.
 */

export type GeoPosition = {
  latitude: number;
  longitude: number;
  accuracyMeters: number;
};

export type GeoErrorCode =
  | 'unsupported'         // navigator.geolocation не существует
  | 'permission_denied'   // юзер отказал
  | 'position_unavailable' // GPS off / нет WiFi / etc.
  | 'timeout';            // не уложились в timeout

export type GeoError = {
  code: GeoErrorCode;
  message: string;
};

export type GeoStatus = 'idle' | 'requesting' | 'success' | 'error';

export type UseGeolocationResult = {
  status: GeoStatus;
  position: GeoPosition | null;
  error: GeoError | null;
  /** Запустить запрос геолокации. Если уже идёт — игнорируется. */
  request: () => void;
  /** Сбросить state до idle. */
  reset: () => void;
};

/**
 * Стратегия запроса:
 *  1. Сначала high-accuracy с коротким таймаутом (8s) — пытаемся
 *     получить честный GPS-fix, если есть GPS-чип.
 *  2. На TIMEOUT (но не на PERMISSION_DENIED) — фоллбэк на
 *     low-accuracy с большим таймаутом (20s) и допуском кэша 1мин.
 *     На desktop без GPS-чипа Chrome определяет по WiFi-сетям —
 *     этому достаточно low-accuracy.
 *
 * Для типичного сценария 'поправить координаты могилы' точность
 * важна только когда юзер физически у могилы с телефоном; на
 * desktop приближённая позиция — норма.
 */
const HIGH_ACCURACY_OPTIONS: PositionOptions = {
  enableHighAccuracy: true,
  timeout: 8_000,
  maximumAge: 0,
};

const LOW_ACCURACY_OPTIONS: PositionOptions = {
  enableHighAccuracy: false,
  timeout: 20_000,
  maximumAge: 60_000,
};

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
          'Не удалось определить местоположение за отведённое время. На телефоне выйдите на открытое место, на desktop попробуйте ещё раз.',
      };
    default:
      return { code: 'position_unavailable', message: err.message };
  }
}

export function useGeolocation(): UseGeolocationResult {
  const [status, setStatus] = useState<GeoStatus>('idle');
  const [position, setPosition] = useState<GeoPosition | null>(null);
  const [error, setError] = useState<GeoError | null>(null);

  const request = useCallback(() => {
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

    const onSuccess = (pos: GeolocationPosition) => {
      setPosition({
        latitude: pos.coords.latitude,
        longitude: pos.coords.longitude,
        accuracyMeters: pos.coords.accuracy,
      });
      setStatus('success');
    };

    const onLowAccuracyError = (err: GeolocationPositionError) => {
      setError(mapBrowserError(err));
      setStatus('error');
    };

    // 1. Пытаемся high-accuracy с коротким таймаутом.
    navigator.geolocation.getCurrentPosition(
      onSuccess,
      (err) => {
        // PERMISSION_DENIED не лечится фоллбэком — сразу отдаём ошибку.
        if (err.code === err.PERMISSION_DENIED) {
          setError(mapBrowserError(err));
          setStatus('error');
          return;
        }
        // 2. TIMEOUT/POSITION_UNAVAILABLE — пробуем low-accuracy.
        navigator.geolocation.getCurrentPosition(
          onSuccess,
          onLowAccuracyError,
          LOW_ACCURACY_OPTIONS,
        );
      },
      HIGH_ACCURACY_OPTIONS,
    );
  }, [status]);

  const reset = useCallback(() => {
    setStatus('idle');
    setPosition(null);
    setError(null);
  }, []);

  return { status, position, error, request, reset };
}
