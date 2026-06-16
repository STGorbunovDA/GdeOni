import { apiClient, unwrap } from '../client';
import type { ApiEnvelope, PagedResponse } from '../types';

/**
 * F6. Эндпоинты карточек умерших. Сейчас — только поиск/листинг.
 * Реальное создание, правка, медиа добавляются в F8 / F11 / F13.
 */

/**
 * Поля DTO один-в-один с backend
 * GdeOni.Application.DeceasedRecords.Queries.GetAll.Model.GetAllDeceasedItemResponse.
 */
export type DeceasedListItem = {
  id: string;
  fullName: string;
  birthDate: string | null;  // 'yyyy-MM-dd'
  deathDate: string;         // 'yyyy-MM-dd'
  hasBurialLocation: boolean;
  latitude: number | null;
  longitude: number | null;
  accuracyMeters: number | null;
  country: string | null;
  city: string | null;
  cemeteryName: string | null;
  plotNumber: string | null;
  graveNumber: string | null;
  isVerified: boolean;
  createdAtUtc: string;
  mainMediaId: string | null;
  /**
   * D36. Bucket + storage key главного фото. Клиент строит URL через
   * buildMediaUrl(bucket, storageKey) из ../utils/mediaUrl.
   */
  mainPhotoBucket: string | null;
  mainPhotoStorageKey: string | null;
  /**
   * @deprecated D36: абсолютный URL с серверным хостом. Используй
   * mainPhotoBucket + mainPhotoStorageKey. Поле сохраняется на 1-2
   * релизных цикла для обратной совместимости.
   */
  mainPhotoUrl: string | null;
};

/**
 * Параметры поиска. Все опциональные кроме page/pageSize.
 * Дата отдаётся строкой 'yyyy-MM-dd' (бэк парсит как DateOnly).
 */
export type SearchDeceasedParams = {
  search?: string;
  firstName?: string;
  lastName?: string;
  middleName?: string;
  country?: string;
  city?: string;
  isVerified?: boolean;
  /** 'yyyy-MM-dd' */
  birthDate?: string;
  /** 'yyyy-MM-dd' */
  deathDate?: string;
  page?: number;
  pageSize?: number;
};

export const deceasedApi = {
  /**
   * GET /api/deceased-records — поиск/листинг карточек.
   * D15: доступен любому авторизованному юзеру.
   * Возвращает PagedResponse с items + totalCount + page + pageSize.
   */
  async search(
    params: SearchDeceasedParams,
  ): Promise<PagedResponse<DeceasedListItem>> {
    return unwrap(
      apiClient.get<ApiEnvelope<PagedResponse<DeceasedListItem>>>(
        '/api/deceased-records',
        { params: cleanParams(params) },
      ),
    );
  },
};

/**
 * Чистим пустые строки и null'ы перед отправкой. Иначе бэк увидит
 * search='' и будет фильтровать по пустой строке.
 */
function cleanParams(p: SearchDeceasedParams): Record<string, string | number | boolean> {
  const out: Record<string, string | number | boolean> = {};
  for (const [key, value] of Object.entries(p)) {
    if (value === undefined || value === null || value === '') continue;
    out[key] = value;
  }
  return out;
}
