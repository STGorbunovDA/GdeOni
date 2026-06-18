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
 * Dev-only workaround: локально бэк сконфигурирован под Android-эмулятор
 * (Minio:PublicBaseUrl=http://10.0.2.2:9000). Web-браузер в Windows
 * этот IP не понимает — подменяем на localhost.
 *
 * На production бэк отдаёт публичный домен MinIO/CDN
 * (https://files.gdeoni.ru) — этот хост не матчится, функция становится
 * no-op.
 */
function applyDevHostFix(url: string): string {
  return url
    .replace(/^http:\/\/10\.0\.2\.2(?=[:/]|$)/, 'http://localhost')
    .replace(/^https:\/\/10\.0\.2\.2(?=[:/]|$)/, 'https://localhost');
}
