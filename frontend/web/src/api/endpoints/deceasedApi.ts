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
  /** ISO datetime — от какого CreatedAtUtc показывать (F17.15). */
  createdFrom?: string;
  /** ISO datetime — до какого CreatedAtUtc показывать (F17.15). */
  createdTo?: string;
  page?: number;
  pageSize?: number;
};

/**
 * Запись воспоминания. Зеркало DeceasedMemoryResponse на бэке.
 * D14: модерация отключена — все записи всегда Approved. Поле
 * moderationStatus оставлено для совместимости с админкой (F17).
 *
 * authorUserId может быть null если автор удалил аккаунт — UI должен
 * отображать "Аноним" в таком случае.
 */
export type DeceasedMemory = {
  id: string;
  text: string;
  authorUserId: string | null;
  /**
   * Отображаемое имя автора (FullName ?? UserName). Null если юзер
   * удалён или backend не нашёл; UI отрисует «Аноним». Заполняется
   * бэком из batch-выборки User.GetDisplayNamesByIds — отдельный запрос
   * клиент не делает.
   */
  authorName: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  moderationStatus: string;
};

/**
 * Полная карточка умершего, GET /api/deceased-records/{id}.
 * Зеркало backend DeceasedDetailsResponse.
 *
 * D36: бэк отдаёт bucket+storageKey, клиент строит URL через
 * buildMediaUrl(mediaBaseUrl, bucket, key). Старое mainPhotoUrl
 * сохранено как deprecated на 1-2 релизных цикла.
 */
export type DeceasedDetails = {
  id: string;
  firstName: string;
  lastName: string;
  middleName: string | null;
  fullName: string;
  birthDate: string | null;
  deathDate: string;
  hasBurialLocation: boolean;
  latitude: number | null;
  longitude: number | null;
  accuracyMeters: number | null;
  country: string | null;
  region: string | null;
  city: string | null;
  cemeteryName: string | null;
  plotNumber: string | null;
  graveNumber: string | null;
  accuracy: string | null;
  shortDescription: string | null;
  biography: string | null;
  createdByUserId: string;
  isVerified: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  mainMediaId: string | null;
  mainPhotoBucket: string | null;
  mainPhotoStorageKey: string | null;
  /** @deprecated D36: используй mainPhotoBucket+mainPhotoStorageKey. */
  mainPhotoUrl: string | null;
  memories: DeceasedMemory[];
};

/**
 * Запрос POST /api/deceased-records/at-grave (F8). Зеркало
 * AddDeceasedAtGraveRequest на бэке. Бэк парсит RelationshipType как
 * enum, но из-за JsonStringEnumConverter принимает имя строкой
 * ('Friend', 'Parent', и т.д.) — поэтому шлём строкой.
 *
 * Даты — 'yyyy-MM-dd' (DateOnly на бэке).
 */
export type AtGraveLocationRequest = {
  latitude: number;
  longitude: number;
  accuracyMeters: number | null;
  country: string | null;
  city: string | null;
  cemeteryName: string | null;
  plotNumber: string | null;
  graveNumber: string | null;
};

export type AtGraveTrackingRequest = {
  relationshipType: string;
  personalNotes: string | null;
  notifyOnDeathAnniversary: boolean;
  notifyOnBirthAnniversary: boolean;
};

export type AtGraveRequest = {
  firstName: string;
  lastName: string;
  middleName: string | null;
  birthDate: string | null;
  deathDate: string;
  shortDescription: string | null;
  biography: string | null;
  graveLocation: AtGraveLocationRequest;
  tracking: AtGraveTrackingRequest;
};

export type AtGraveResponse = {
  deceasedId: string;
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

  /**
   * GET /api/deceased-records/{id} — полная карточка. Используется
   * на preview (F7) и при загрузке деталей админом. Обычный юзер тоже
   * может получить её, если он трекает или если карточка не удалена —
   * фильтры доступа делает бэк.
   */
  async getById(id: string): Promise<DeceasedDetails> {
    return unwrap(
      apiClient.get<ApiEnvelope<DeceasedDetails>>(
        `/api/deceased-records/${id}`,
      ),
    );
  },

  /**
   * POST /api/deceased-records/at-grave (F8). Атомарно создаёт карточку
   * + место захоронения + tracking-запись текущего юзера. Возвращает id
   * созданной карточки.
   */
  async addAtGrave(request: AtGraveRequest): Promise<AtGraveResponse> {
    return unwrap(
      apiClient.post<ApiEnvelope<AtGraveResponse>>(
        '/api/deceased-records/at-grave',
        request,
      ),
    );
  },

  /**
   * F15. PUT /api/deceased-records/{id}/burial-location/from-gps — правка
   * только координат, без затирания Country/City/Cemetery/Plot/Grave.
   * Эндпойнт исторически называется /from-gps, но (после E20) сохраняет
   * адресные поля. На бэке ICanEditDeceasedPolicy: 403 для не-автора и
   * не-админа.
   */
  async setBurialFromGps(
    deceasedId: string,
    request: { latitude: number; longitude: number; accuracyMeters: number | null },
  ): Promise<void> {
    await unwrap(
      apiClient.put<ApiEnvelope<{ deceasedId: string }>>(
        `/api/deceased-records/${deceasedId}/burial-location/from-gps`,
        request,
      ),
    );
  },

  /**
   * F17.2. DELETE /api/deceased-records/{id} — безвозвратное удаление
   * карточки. Доступно только админам (Roles="SuperAdmin,Admin"). После
   * удаления каскадно уходят tracking-записи всех юзеров, воспоминания
   * и media — это решение бэка, фронт просто инвалидирует кэши списков.
   */
  async remove(id: string): Promise<void> {
    await unwrap(
      apiClient.delete<ApiEnvelope<{ deceasedId: string }>>(
        `/api/deceased-records/${id}`,
      ),
    );
  },

  /**
   * F17.3. PUT /api/deceased-records/{id}/verify — пометить карточку
   * как проверенную редакцией. Admin-only. Возвращает 409 если уже
   * verified — UI просто покажет ошибку через formatError.
   */
  async verify(id: string): Promise<void> {
    await unwrap(
      apiClient.put<ApiEnvelope<{ deceasedId: string }>>(
        `/api/deceased-records/${id}/verify`,
        {},
      ),
    );
  },

  /**
   * F17.3. PUT /api/deceased-records/{id}/unverified — снять отметку
   * проверки. Admin-only. URL содержит historic `unverified` (past
   * participle), а не `unverify` — обратной совместимости ради не
   * переименовываем на бэке.
   */
  async unverify(id: string): Promise<void> {
    await unwrap(
      apiClient.put<ApiEnvelope<{ deceasedId: string }>>(
        `/api/deceased-records/${id}/unverified`,
        {},
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
