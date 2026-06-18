/**
 * F14.2. Утилиты для multi-point маршрута. Зеркало
 * RouteViewModel.OptimizeOrder + ExternalMapsService на mobile.
 *
 * По решению 2026-05-13 на UI используется только Яндекс — Google и
 * 2ГИС оставлены билдерами рядом (на случай возврата выбора провайдера
 * через локальную правку без нового деплоя).
 */

export type Point = {
  /** Уникальный id (для UI ключей и snake-протоколирования). */
  id: string;
  latitude: number;
  longitude: number;
  /** ФИО умершего — для подсказки в UI. */
  label?: string;
};

/**
 * Haversine — расстояние между двумя точками на сфере в метрах.
 * R = 6371000 (средний радиус Земли). Точность достаточна для
 * сортировки точек по близости — не для геодезических расчётов.
 */
export function haversine(a: Point, b: Point): number {
  const R = 6371000;
  const toRad = (d: number) => (d * Math.PI) / 180;
  const dLat = toRad(b.latitude - a.latitude);
  const dLon = toRad(b.longitude - a.longitude);
  const lat1 = toRad(a.latitude);
  const lat2 = toRad(b.latitude);
  const h =
    Math.sin(dLat / 2) ** 2 +
    Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLon / 2) ** 2;
  return 2 * R * Math.asin(Math.sqrt(h));
}

/**
 * Nearest-neighbor TSP. Не оптимальный (NP-полная задача), но для
 * 5-10 точек даёт хороший результат за O(n²). Зеркало
 * RouteViewModel.OptimizeOrder.
 *
 * Если origin есть — начинаем от него (он НЕ попадает в выход —
 * это текущее положение юзера). Иначе берём первую точку как старт
 * и она попадает в выход.
 */
export function optimizeOrder(
  origin: Point | null,
  points: Point[],
): Point[] {
  if (points.length <= 1) return points;

  const remaining = [...points];
  const ordered: Point[] = [];
  let current: Point;
  if (origin) {
    current = origin;
  } else {
    current = remaining.shift()!;
    ordered.push(current);
  }

  while (remaining.length > 0) {
    let bestIdx = 0;
    let bestDist = Infinity;
    for (let i = 0; i < remaining.length; i++) {
      const d = haversine(current, remaining[i]);
      if (d < bestDist) {
        bestDist = d;
        bestIdx = i;
      }
    }
    ordered.push(remaining[bestIdx]);
    current = remaining[bestIdx];
    remaining.splice(bestIdx, 1);
  }
  return ordered;
}

/**
 * Формат координат: всегда `.toFixed(6)`. `String(lat)` нельзя —
 * у больших значений уходит в экспоненту.
 */
function fmt(n: number): string {
  return n.toFixed(6);
}

/**
 * Яндекс deep-link: rtext=lat,lon~lat,lon. rtt=auto.
 * Зеркало ExternalMapsService.BuildYandexUrl на mobile.
 *
 * Если origin=null — оставляем первый сегмент пустым (~lat,lon~lat,lon),
 * чтобы Яндекс показал панель маршрута с пустым "Откуда" (юзер ткнёт
 * "Моё местоположение" — там нативное определение точнее браузерного).
 * Без этого Яндекс молча использует первую точку как origin, что
 * сбивает юзера ("где моё местоположение?").
 *
 * ll=lon,lat&z=17 — центрирует карту на первой точке маршрута с
 * детальным зумом. Без этого Яндекс может выбрать z=19 на короткий
 * сегмент или z=4 на длинный — в обоих случаях ориентир теряется.
 */
export function buildYandexUrl(
  origin: Point | null,
  points: Point[],
): string {
  if (points.length === 0) return 'https://yandex.ru/maps/';

  const allCoords = origin
    ? [origin, ...points].map((p) => `${fmt(p.latitude)},${fmt(p.longitude)}`)
    : ['', ...points.map((p) => `${fmt(p.latitude)},${fmt(p.longitude)}`)];
  const rtext = allCoords.join('~');

  // Центр карты — на первой реальной точке (origin или первый из points).
  const center = origin ?? points[0];
  const lat = fmt(center.latitude);
  const lon = fmt(center.longitude);

  return `https://yandex.ru/maps/?rtext=${rtext}&rtt=auto&ll=${lon},${lat}&z=17`;
}

/**
 * Fallback для F14.1 когда браузерная геолокация не сработала. Открываем
 * Яндекс Карты с УЖЕ ОТКРЫТОЙ панелью маршрута и заполненным "Куда".
 * Поле "Откуда" остаётся пустым — юзер ткнёт в него и Яндекс предложит
 * "Моё местоположение" (там нативное определение работает лучше
 * браузерного API).
 *
 * Параметры:
 *  - rtext=~lat,lon — одна точка = destination, origin пустой.
 *  - rtt=auto — режим "на машине".
 *  - ll=lon,lat — центр карты. ВНИМАНИЕ: Yandex принимает lon,lat
 *    в обратном порядке! Без явного ll Яндекс ставит zoom-4 на
 *    середину мира (неудобно, юзеру не видно точки).
 *  - z=17 — детальный городской зум, видна конкретная аллея/подъезд
 *    на кладбище (z=16 даёт квартал — далековато; z=18+ уже отдельные
 *    деревья, ориентир теряется).
 */
export function buildYandexLookupUrl(point: Point): string {
  const lat = fmt(point.latitude);
  const lon = fmt(point.longitude);
  return `https://yandex.ru/maps/?rtext=~${lat},${lon}&rtt=auto&ll=${lon},${lat}&z=17`;
}

/**
 * Google deep-link. Оставлен на случай возврата выбора провайдера.
 * destination = последняя точка, остальные — waypoints.
 */
export function buildGoogleUrl(
  origin: Point | null,
  points: Point[],
): string {
  if (points.length === 0) return 'https://www.google.com/maps/';
  const destination = points[points.length - 1];
  const waypoints = points
    .slice(0, -1)
    .map((p) => `${fmt(p.latitude)},${fmt(p.longitude)}`)
    .join('|');
  const params = new URLSearchParams({
    api: '1',
    travelmode: 'driving',
    destination: `${fmt(destination.latitude)},${fmt(destination.longitude)}`,
  });
  if (origin) {
    params.set('origin', `${fmt(origin.latitude)},${fmt(origin.longitude)}`);
  } else {
    params.set('origin', '');
  }
  if (waypoints) params.set('waypoints', waypoints);
  return `https://www.google.com/maps/dir/?${params.toString()}`;
}

/**
 * 2ГИС deep-link. ВНИМАНИЕ: 2ГИС хочет lon,lat (обратный порядок!).
 */
export function build2GisUrl(
  origin: Point | null,
  points: Point[],
): string {
  const all = origin ? [origin, ...points] : points;
  const segments = all.map((p) => `${fmt(p.longitude)},${fmt(p.latitude)}`).join('|');
  return `https://2gis.ru/routeSearch/rsType/car/points/${segments}`;
}
