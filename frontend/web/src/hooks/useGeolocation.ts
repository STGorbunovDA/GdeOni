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

const DEFAULT_OPTIONS: PositionOptions = {
  enableHighAccuracy: true,
  timeout: 15_000,
  maximumAge: 0,
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
          'Превышено время ожидания GPS. Попробуйте ещё раз на открытом месте.',
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

    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setPosition({
          latitude: pos.coords.latitude,
          longitude: pos.coords.longitude,
          accuracyMeters: pos.coords.accuracy,
        });
        setStatus('success');
      },
      (err) => {
        setError(mapBrowserError(err));
        setStatus('error');
      },
      DEFAULT_OPTIONS,
    );
  }, [status]);

  const reset = useCallback(() => {
    setStatus('idle');
    setPosition(null);
    setError(null);
  }, []);

  return { status, position, error, request, reset };
}
