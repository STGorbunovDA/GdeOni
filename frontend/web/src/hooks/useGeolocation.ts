import { useCallback, useState } from 'react';

/**
 * F5. Браузерная геолокация. Зеркало mobile GeolocationService.
 *
 * Особенности браузерной геолокации:
 *  - Permission prompt браузер показывает сам (юзер кликнул кнопку
 *    → браузер всплывает с разрешением).
 *  - На production обязателен HTTPS. На localhost работает по HTTP.
 *  - Permanently denied → второй request тоже сразу падает с
 *    PERMISSION_DENIED. UI подсказывает про «замочек» в адресной строке.
 *
 * Стратегия ТОЧНОСТИ (важно для координат могилы):
 *  1. Точный fix через watchPosition + enableHighAccuracy. GPS уточняется
 *     со временем: первый замер часто грубый (сеть/A-GPS), затем accuracy
 *     падает до единиц метров. Поэтому НЕ берём первый попавшийся, а держим
 *     ЛУЧШИЙ (минимальная accuracy) в течение MAX_WATCH_MS и останавливаемся
 *     раньше, как только accuracy ≤ GOOD_ACCURACY_M.
 *  2. Если за это время GPS вообще не дал точку (desktop без GPS, сигнала
 *     нет) — фоллбэк на грубую сетевую позицию (WiFi/IP/кэш), чтобы юзер
 *     хоть что-то получил и допоправил на карте.
 *
 * PERMISSION_DENIED на любом шаге → сразу ошибка, фоллбэк не поможет.
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

/** Точность, при которой перестаём ждать улучшения GPS (метры). */
const GOOD_ACCURACY_M = 20;
/** Максимум ждём улучшения точного fix'а (мс). */
const MAX_WATCH_MS = 15_000;

/** Фоллбэк для устройств без GPS: грубая сетевая позиция / кэш. */
const FALLBACK_ATTEMPTS: PositionOptions[] = [
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

function getPositionAsync(options: PositionOptions): Promise<GeolocationPosition> {
  return new Promise((resolve, reject) => {
    navigator.geolocation.getCurrentPosition(resolve, reject, options);
  });
}

type WatchOutcome = {
  position: GeolocationPosition | null;
  deniedError: GeolocationPositionError | null;
};

/**
 * Собирает самый точный fix через watchPosition. GPS уточняется со временем,
 * поэтому держим замер с минимальной accuracy и завершаем, как только он
 * достаточно точен (≤ GOOD_ACCURACY_M) или истёк MAX_WATCH_MS. При
 * PERMISSION_DENIED — сразу выходим, это не лечится ожиданием.
 */
function watchBestPosition(): Promise<WatchOutcome> {
  return new Promise((resolve) => {
    let best: GeolocationPosition | null = null;
    let deniedError: GeolocationPositionError | null = null;
    let watchId: number | null = null;
    let settled = false;

    const finish = () => {
      if (settled) return;
      settled = true;
      if (watchId !== null) navigator.geolocation.clearWatch(watchId);
      clearTimeout(timer);
      resolve({ position: best, deniedError });
    };

    const timer = setTimeout(finish, MAX_WATCH_MS);

    watchId = navigator.geolocation.watchPosition(
      (pos) => {
        if (!best || pos.coords.accuracy < best.coords.accuracy) {
          best = pos;
        }
        // Уже точно достаточно — не ждём дальше.
        if (pos.coords.accuracy <= GOOD_ACCURACY_M) finish();
      },
      (err) => {
        // Отказ в доступе ожиданием не лечится — выходим сразу. Прочие
        // ошибки (unavailable/timeout) не рушат: ждём таймер, вдруг GPS
        // ещё «прогреется» и даст fix.
        if (err.code === err.PERMISSION_DENIED) {
          deniedError = err;
          finish();
        }
      },
      { enableHighAccuracy: true, maximumAge: 0, timeout: MAX_WATCH_MS },
    );
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

    // 1) Точный fix (лучший из потока watchPosition).
    const { position: best, deniedError } = await watchBestPosition();
    if (deniedError) {
      setError(mapBrowserError(deniedError));
      setStatus('error');
      return;
    }
    if (best) {
      setPosition({
        latitude: best.coords.latitude,
        longitude: best.coords.longitude,
        accuracyMeters: best.coords.accuracy,
      });
      setStatus('success');
      return;
    }

    // 2) GPS ничего не дал — фоллбэк на грубую сетевую позицию.
    let lastError: GeolocationPositionError | null = null;
    for (const opts of FALLBACK_ATTEMPTS) {
      try {
        const pos = await getPositionAsync(opts);
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
        if (err.code === err.PERMISSION_DENIED) break;
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
