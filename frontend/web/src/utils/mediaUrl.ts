/**
 * D36 / F6. Построение media URL на клиенте.
 *
 * Бэк отдаёт bucket+storage_key в DTO листингов и базовый URL хранилища
 * в /api/app/features.mediaBaseUrl. Каждый клиент сам строит финальный
 * URL под свою сеть — web→localhost, Android-эмулятор→10.0.2.2,
 * production→CDN-домен. Это снимает проблему "один URL для всех клиентов".
 *
 * Использование:
 *   const features = useAppFeatures();
 *   const url = buildMediaUrl(features.data?.mediaBaseUrl, item.mainPhotoBucket, item.mainPhotoStorageKey);
 */
export function buildMediaUrl(
  mediaBaseUrl: string | undefined,
  bucket: string | null | undefined,
  storageKey: string | null | undefined,
): string | null {
  if (!mediaBaseUrl || !bucket || !storageKey) return null;
  const base = mediaBaseUrl.replace(/\/+$/, '');
  return `${base}/${bucket}/${encodeURIComponent(storageKey)}`;
}
