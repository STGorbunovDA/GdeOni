import { apiClient, unwrap } from '../client';
import type { ApiEnvelope, PagedResponse } from '../types';

/**
 * F13. Эндпоинты медиа-галерей. Чтение доступно всем авторизованным,
 * запись (upload/delete/main-photo) — admin-only (D26 на бэке).
 *
 * D36: бэк отдаёт bucket+storageKey, клиент собирает URL для фото через
 * buildMediaUrl (см. mediaUrl.ts). Для документов поле url — presigned,
 * клиент использует его как есть.
 */

/** Зеркало MediaKind enum на бэке (FromForm принимает int). */
export const MediaKinds = {
  DeceasedPhoto: 1,
  GravePhoto: 2,
  Document: 3,
} as const;

export type MediaKindValue =
  (typeof MediaKinds)[keyof typeof MediaKinds];

/** Имена kind в ответе списка (бэк сериализует enum через ToString). */
export const MediaKindNames = {
  DeceasedPhoto: 'DeceasedPhoto',
  GravePhoto: 'GravePhoto',
  Document: 'Document',
} as const;

/** Зеркало MediaListItemResponse. */
export type MediaListItem = {
  id: string;
  deceasedId: string;
  uploadedByUserId: string;
  kind: 'DeceasedPhoto' | 'GravePhoto' | 'Document' | 'Other';
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  description: string | null;
  isMainPhoto: boolean;
  moderationStatus: string;
  bucket: string;
  storageKey: string;
  /** Presigned для документов; для фото — public URL (deprecated, сборка через bucket+key). */
  url: string;
  isPresigned: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
};

/** Лимиты валидации — зеркало FileValidator на бэке. */
export const MEDIA_LIMITS = {
  maxPhotoSizeBytes: 10 * 1024 * 1024,
  maxDocumentSizeBytes: 25 * 1024 * 1024,
  allowedPhotoTypes: ['image/jpeg', 'image/png', 'image/webp'] as string[],
  allowedDocumentTypes: ['application/pdf'] as string[],
};

type UploadProgress = {
  loaded: number;
  total: number;
};

export const mediaApi = {
  /**
   * GET /api/deceased-records/{id}/media?kind=&page=&pageSize=. Без
   * параметра kind возвращает все media. Бэк фильтрует ModerationStatus
   * по правам (см. D11.13.1) — обычному юзеру отдаёт только Approved.
   */
  async list(
    deceasedId: string,
    kind: MediaKindValue | undefined,
    page = 1,
    pageSize = 100,
  ): Promise<PagedResponse<MediaListItem>> {
    return unwrap(
      apiClient.get<ApiEnvelope<PagedResponse<MediaListItem>>>(
        `/api/deceased-records/${deceasedId}/media`,
        { params: kind !== undefined ? { kind, page, pageSize } : { page, pageSize } },
      ),
    );
  },

  /**
   * POST /api/deceased-records/{id}/media — multipart/form-data.
   * onProgress используется для axios.onUploadProgress (Mantine Progress).
   */
  async upload(
    deceasedId: string,
    file: File,
    kind: MediaKindValue,
    description: string | null,
    onProgress?: (p: UploadProgress) => void,
  ): Promise<{ mediaId: string }> {
    const form = new FormData();
    form.append('file', file);
    form.append('kind', String(kind));
    if (description) form.append('description', description);

    return unwrap(
      apiClient.post<ApiEnvelope<{ mediaId: string }>>(
        `/api/deceased-records/${deceasedId}/media`,
        form,
        {
          headers: { 'Content-Type': 'multipart/form-data' },
          onUploadProgress: (e) => {
            if (onProgress && e.total) {
              onProgress({ loaded: e.loaded, total: e.total });
            }
          },
        },
      ),
    );
  },

  /**
   * DELETE /api/deceased-records/{id}/media/{mediaId}. Бэк возвращает
   * 204 No Content (без envelope) — используем apiClient напрямую.
   */
  async remove(deceasedId: string, mediaId: string): Promise<void> {
    await apiClient.delete(
      `/api/deceased-records/${deceasedId}/media/${mediaId}`,
    );
  },

  /**
   * PATCH /api/deceased-records/{id}/media/{mediaId}/main-photo. 204 No Content.
   * Только для DeceasedPhoto (бэк вернёт 409 для других kind).
   */
  async setMain(deceasedId: string, mediaId: string): Promise<void> {
    await apiClient.patch(
      `/api/deceased-records/${deceasedId}/media/${mediaId}/main-photo`,
    );
  },

  /**
   * F13. Скачивает файл через бэк-прокси (см. DownloadMediaUseCase).
   * Нужен для документов: presigned URL'ы в dev-конфиге содержат
   * host 10.0.2.2:9000 (Android-эмулятор), который web в Windows не
   * разрешает. Бэк сам тянет поток из MinIO, axios получает blob,
   * клиент открывает его через blob: URL → window.open.
   *
   * Bearer-токен подставится автоматически через request interceptor —
   * простой `window.open('/api/.../download')` без него не работает.
   */
  async downloadBlob(deceasedId: string, mediaId: string): Promise<Blob> {
    const response = await apiClient.get<Blob>(
      `/api/deceased-records/${deceasedId}/media/${mediaId}/download`,
      { responseType: 'blob' },
    );
    return response.data;
  },
};
