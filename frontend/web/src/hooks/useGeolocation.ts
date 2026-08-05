import { useCallback, useEffect, useRef, useState } from 'react';
import { useAppFeatures } from './useAppFeatures';

/**
 * F5. Браузерная геолокация — стратегия «сбор лучшего fix за окно».
 *
 * Почему не «первый fix»: getCurrentPosition отдаёт САМЫЙ первый ответ GPS,
 * а он почти всегда худший — чип «холодный», видит мало спутников (30-100 м),
 * либо это вообще WiFi-позиция. GPS дозревает за 10-30 секунд. Поэтому мы:
 *
 *  1. Через watchPosition (enableHighAccuracy, maximumAge 0) собираем замеры
 *     в течение окна (по умолчанию 60 с; значение приходит с бэка —
 *     Geolocation:AcquireWindowSeconds, меняется без пересборки фронта).
 *  2. Всё время держим ЛУЧШИЙ по coords.accuracy (минимальный радиус).
 *  3. Если точность достигла TARGET_ACCURACY_M — останавливаемся раньше.
 *  4. По истечении окна отдаём лучший собранный fix.
 *
 * currentAccuracy обновляется по ходу — оверлей показывает «текущая точность».
 *
 * Оговорки:
 *  - TARGET_ACCURACY_M = 2 м — это ПОРОГ ранней остановки-«мечты». Реально
 *    телефон на улице даёт 5-20 м, десктоп без GPS — сотни метров (WiFi/IP),
 *    и усреднение/ожидание этого не лечит. Поэтому рядом всегда остаётся
 *    ручной сдвиг маркера на карте.
 *  - PERMISSION_DENIED не лечится ничем — сразу ошибка.
 *  - Если за окно не пришло НИ ОДНОГО fix (desktop без GPS / VPN-таймаут) —
 *    один низкоточный фолбэк с кэшем, чтобы отдать «хоть что-то».
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
  /** Лучшая достигнутая точность (м) на текущий момент — для оверлея во время сбора. */
  currentAccuracy: number | null;
  error: GeoError | null;
  request: () => void;
  reset: () => void;
  /**
   * «Пропустить»: не ждать остаток окна — взять лучший собранный на данный
   * момент fix (status → success). No-op, если ещё ни одного замера не пришло.
   */
  accept: () => void;
  /** Длина окна сбора (сек) — для текста оверлея «это до N секунд». */
  windowSeconds: number;
};

/** Фолбэк порога ранней остановки (м), если конфиг с бэка ещё не загрузился. */
const DEFAULT_TARGET_ACCURACY_M = 0.5;
/** Фолбэк окна сбора (сек), если конфиг с бэка ещё не загрузился. */
const DEFAULT_WINDOW_SECONDS = 60;

/**
 * Фолбэк, если высокоточный сбор не дал ни одного fix (нет GPS-чипа /
 * VPN-таймаут): один низкоточный запрос с кэшем — «хоть что-то».
 */
const FALLBACK_ATTEMPT: PositionOptions = {
  enableHighAccuracy: false,
  timeout: 10_000,
  maximumAge: Infinity,
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

export function useGeolocation(): UseGeolocationResult {
  const [status, setStatus] = useState<GeoStatus>('idle');
  const [position, setPosition] = useState<GeoPosition | null>(null);
  const [currentAccuracy, setCurrentAccuracy] = useState<number | null>(null);
  const [error, setError] = useState<GeoError | null>(null);

  // Окно сбора приходит с бэка (Geolocation:AcquireWindowSeconds) — меняется
  // без пересборки фронта. Пока не загрузилось / аноним — фолбэк 60 с.
  const features = useAppFeatures();
  const windowSeconds =
    features.data?.geoAcquireWindowSeconds ?? DEFAULT_WINDOW_SECONDS;
  const windowMs = windowSeconds * 1000;
  // Порог ранней остановки (м) — тоже из конфига (Geolocation:TargetAccuracyMeters).
  const targetAccuracyM =
    features.data?.geoTargetAccuracyMeters ?? DEFAULT_TARGET_ACCURACY_M;

  const watchIdRef = useRef<number | null>(null);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const bestRef = useRef<GeoPosition | null>(null);
  const finishedRef = useRef(false);

  const cleanup = useCallback(() => {
    if (watchIdRef.current !== null) {
      navigator.geolocation.clearWatch(watchIdRef.current);
      watchIdRef.current = null;
    }
    if (timerRef.current !== null) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }
  }, []);

  // Останавливаем watch/timer при размонтировании — иначе колбэки после
  // ухода со страницы дёргают setState на размонтированном компоненте.
  useEffect(() => cleanup, [cleanup]);

  const request = useCallback(() => {
    if (status === 'requesting') return;

    if (!('geolocation' in navigator)) {
      setError({ code: 'unsupported', message: 'Браузер не поддерживает геолокацию.' });
      setStatus('error');
      return;
    }

    cleanup();
    bestRef.current = null;
    finishedRef.current = false;
    setPosition(null);
    setCurrentAccuracy(null);
    setError(null);
    setStatus('requesting');

    const finishSuccess = (best: GeoPosition) => {
      if (finishedRef.current) return;
      finishedRef.current = true;
      cleanup();
      setPosition(best);
      setCurrentAccuracy(best.accuracyMeters);
      setStatus('success');
    };

    const finishError = (err: GeoError) => {
      if (finishedRef.current) return;
      finishedRef.current = true;
      cleanup();
      setError(err);
      setStatus('error');
    };

    watchIdRef.current = navigator.geolocation.watchPosition(
      (pos) => {
        const fix: GeoPosition = {
          latitude: pos.coords.latitude,
          longitude: pos.coords.longitude,
          accuracyMeters: pos.coords.accuracy,
        };
        // Держим лучший по точности (меньший радиус — лучше).
        if (
          bestRef.current === null ||
          fix.accuracyMeters < bestRef.current.accuracyMeters
        ) {
          bestRef.current = fix;
          setCurrentAccuracy(fix.accuracyMeters);
        }
        // Достигли порога — не ждём остаток окна. При очень малом пороге
        // (0.5/0.2 м) телефонный GPS его почти не берёт → собираем всё окно.
        if (bestRef.current.accuracyMeters <= targetAccuracyM) {
          finishSuccess(bestRef.current);
        }
      },
      (err) => {
        // Отказ в доступе фолбэком не лечится — выходим сразу. Прочие ошибки
        // во время watch глотаем: watch может ещё дать fix, а если нет —
        // сработает таймер окна с фолбэком.
        if (err.code === err.PERMISSION_DENIED) {
          finishError(mapBrowserError(err));
        }
      },
      { enableHighAccuracy: true, maximumAge: 0, timeout: windowMs },
    );

    timerRef.current = setTimeout(async () => {
      if (finishedRef.current) return;

      // Есть собранный лучший — берём его.
      if (bestRef.current !== null) {
        finishSuccess(bestRef.current);
        return;
      }

      // Ни одного fix за окно — низкоточный фолбэк.
      cleanup();
      try {
        const pos = await getPositionAsync(FALLBACK_ATTEMPT);
        finishSuccess({
          latitude: pos.coords.latitude,
          longitude: pos.coords.longitude,
          accuracyMeters: pos.coords.accuracy,
        });
      } catch (e) {
        finishError(mapBrowserError(e as GeolocationPositionError));
      }
    }, windowMs);
  }, [status, cleanup, windowMs, targetAccuracyM]);

  const reset = useCallback(() => {
    cleanup();
    // Гасим возможные поздние колбэки предыдущего запроса.
    finishedRef.current = true;
    bestRef.current = null;
    setStatus('idle');
    setPosition(null);
    setCurrentAccuracy(null);
    setError(null);
  }, [cleanup]);

  // «Пропустить»: остановиться и взять лучший собранный fix. Если замеров
  // ещё не было — ничего не делаем (в оверлее кнопка тогда заблокирована).
  const accept = useCallback(() => {
    if (finishedRef.current) return;
    const best = bestRef.current;
    if (best === null) return;
    finishedRef.current = true;
    cleanup();
    setPosition(best);
    setCurrentAccuracy(best.accuracyMeters);
    setStatus('success');
  }, [cleanup]);

  return { status, position, currentAccuracy, error, request, reset, accept, windowSeconds };
}
