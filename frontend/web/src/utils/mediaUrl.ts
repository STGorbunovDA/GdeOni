/**
 * D36 / F6. Построение media URL на клиенте.
 *
 * Бэк отдаёт bucket+storage_key в DTO листингов И в details/preview, а
 * базовый URL хранилища — в /api/app/features.mediaBaseUrl. Каждый клиент
 * сам строит финальный URL под свою сеть — web→localhost,
 * Android-эмулятор→10.0.2.2, production→CDN-домен. Это снимает проблему
 * "один URL для всех клиентов".
 */
export function buildMediaUrl(
  mediaBaseUrl: string | undefined,
  bucket: string | null | undefined,
  storageKey: string | null | undefined,
): string | null {
  if (!mediaBaseUrl || !bucket || !storageKey) return null;
  const base = applyDevHostFix(mediaBaseUrl).replace(/\/+$/, '');
  return `${base}/${bucket}/${encodeURIComponent(storageKey)}`;
}

/**
 * Dev-хостовая подмена MinIO-URL.
 *
 * Локально бэк сконфигурирован под Android-эмулятор
 * (Minio:PublicBaseUrl=http://10.0.2.2:9000). Для web-браузера:
 *  - на http-странице (локальный `npm run dev`) браузер достаёт MinIO
 *    напрямую — меняем только 10.0.2.2 → localhost;
 *  - на https-странице (туннель cloudflared) прямой http://localhost:9000 —
 *    это mixed content и вообще недоступен извне. Отдаём SAME-ORIGIN
 *    путь (пустой base → `/<bucket>/<key>`), а Vite/nginx проксирует
 *    его на MinIO (см. vite.tunnel.config.ts).
 *
 * Production отдаёт публичный домен (https://files.gdeoni.ru) — он не
 * матчится как dev-хост, функция становится no-op.
 */
function applyDevHostFix(url: string): string {
  const isDevMinio = /^https?:\/\/(?:10\.0\.2\.2|localhost):9000(?=[/?#]|$)/.test(
    url,
  );
  if (
    isDevMinio &&
    typeof window !== 'undefined' &&
    window.location.protocol === 'https:'
  ) {
    return '';
  }
  return url
    .replace(/^http:\/\/10\.0\.2\.2(?=[:/]|$)/, 'http://localhost')
    .replace(/^https:\/\/10\.0\.2\.2(?=[:/]|$)/, 'https://localhost');
}
