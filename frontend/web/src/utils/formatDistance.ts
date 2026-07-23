/**
 * F36. Форматирование расстояния до могилы. Зеркало mobile
 * GdeOni.Mobile.Shared.Search.DistanceFormatter — бэк отдаёт целые
 * метры, показываем «120 м» до километра и «1.2 км» дальше.
 *
 * Разделитель дробной части — точка (как в mobile через
 * InvariantCulture), чтобы вид не зависел от локали браузера.
 */
export const METERS_TO_KM_THRESHOLD = 1000;

export function formatDistance(meters: number): string {
  const m = Number.isFinite(meters) && meters > 0 ? Math.round(meters) : 0;

  if (m < METERS_TO_KM_THRESHOLD) return `${m} м`;

  // toFixed(1) даст "1.0 км" — лишний ноль убираем, как '{0:0.#}' в C#.
  const km = (m / 1000).toFixed(1).replace(/\.0$/, '');
  return `${km} км`;
}
