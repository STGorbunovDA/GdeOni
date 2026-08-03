import { apiClient, unwrap } from '../client';
import type { ApiEnvelope, PagedResponse } from '../types';
import type { DeceasedDetails } from './deceasedApi';

/**
 * F7. Эндпоинты "мои отслеживаемые" — пока минимум, нужный для preview:
 *  - exists  (для текста кнопки на preview),
 *  - track   (для подписки кнопкой "Добавить в отслеживание").
 * Полный CRUD (list / details / archive / route) будет в F9-F11.
 */

/**
 * Имена RelationshipType, как их понимает бэк
 * (GdeOni.Domain.Shared.RelationshipType + Shared.Relationships.RelationshipTypeValues).
 * Бэк парсит строку case-insensitive и отдаёт её обратно тоже строкой
 * (см. D11.1.1).
 */
export const RelationshipTypes = {
  Parent: 'Parent',
  Grandparent: 'Grandparent',
  GreatGrandfather: 'GreatGrandfather',
  GreatGrandmother: 'GreatGrandmother',
  Child: 'Child',
  Spouse: 'Spouse',
  Sibling: 'Sibling',
  Relative: 'Relative',
  DistantRelative: 'DistantRelative',
  Friend: 'Friend',
  Acquaintance: 'Acquaintance',
  Other: 'Other',
} as const;

export type RelationshipType =
  (typeof RelationshipTypes)[keyof typeof RelationshipTypes];

/**
 * Бэк-DTO: AddMeTrackingRequest. Дефолт на preview-кнопке — Friend +
 * notify off (как на mobile в DeceasedPreviewViewModel.PrimaryActionAsync).
 */
export type AddMeTrackingRequest = {
  relationshipType: RelationshipType;
  personalNotes: string | null;
  notifyOnDeathAnniversary: boolean;
  notifyOnBirthAnniversary: boolean;
};

/**
 * Ответ POST /api/users/me/tracked-deceased/{id} — id записи tracking,
 * не самой карточки. Нам пока нужен только факт успеха.
 */
export type TrackDeceasedResponse = {
  id: string;
};

/**
 * Имена TrackStatus как их шлёт бэк (после D11.1.1 — строкой).
 * F9 фильтрует Archived на клиенте (для архива отдельная страница F10).
 * Muted остаётся в списке — это статус уведомлений, не отслеживания.
 */
export const TrackStatuses = {
  Active: 'Active',
  Muted: 'Muted',
  Archived: 'Archived',
} as const;

export type TrackStatus = (typeof TrackStatuses)[keyof typeof TrackStatuses];

/**
 * Зеркало MyTrackingInfoResponse на бэке — настройки трекинга текущего
 * юзера для одной карточки. Возвращается в getDetails.
 */
export type MyTrackingInfo = {
  trackingId: string;
  relationshipType: string;
  personalNotes: string | null;
  notifyOnDeathAnniversary: boolean;
  notifyOnBirthAnniversary: boolean;
  /** F42. Набор «за сколько дней» напоминать о годовщине смерти (0/1/3/7). */
  deathAnniversaryLeadDays: number[];
  /** F42. Набор «за сколько дней» напоминать о годовщине рождения. */
  birthAnniversaryLeadDays: number[];
  status: TrackStatus;
  trackedAtUtc: string;
};

/**
 * Запрос PATCH /api/users/me/tracked-deceased/{id}. Бэк требует ВСЕ
 * поля сразу (UpdateTrackingUseCase вызывает UpdateTracking + опционально
 * ChangeTrackingStatus). Если поле не передать — затрётся дефолтом.
 * Для смены только статуса — сначала тянем текущий tracking через
 * getDetails и сливаем.
 */
export type UpdateTrackingRequest = {
  relationshipType: string;
  personalNotes: string | null;
  notifyOnDeathAnniversary: boolean;
  notifyOnBirthAnniversary: boolean;
  /**
   * F42. Наборы «за сколько дней» напоминать о годовщинах (0/1/3/7). Если
   * заданы — бэк берёт их (иначе выводит из notifyOn*-флагов). Пустой массив
   * = напоминание выключено.
   */
  deathAnniversaryLeadDays?: number[];
  birthAnniversaryLeadDays?: number[];
  trackStatus: TrackStatus;
};

/**
 * Зеркало MyTrackedDeceasedListItemResponse на бэке.
 *
 * D36: клиент строит URL фото через buildMediaUrl(mediaBaseUrl, bucket, key).
 * mainPhotoUrl сохранён как deprecated на 1-2 релизных цикла.
 */
export type TrackedDeceasedListItem = {
  trackingId: string;
  deceasedId: string;
  fullName: string;
  birthDate: string | null;
  deathDate: string;
  hasGraveLocation: boolean;
  graveLatitude: number | null;
  graveLongitude: number | null;
  mainMediaId: string | null;
  mainPhotoBucket: string | null;
  mainPhotoStorageKey: string | null;
  /** @deprecated D36: используй mainPhotoBucket+mainPhotoStorageKey. */
  mainPhotoUrl: string | null;
  relationshipType: string;
  status: TrackStatus;
  notifyOnDeathAnniversary: boolean;
  notifyOnBirthAnniversary: boolean;
  /** F42. Набор «за сколько дней» напоминать о годовщине смерти (0/1/3/7). */
  deathAnniversaryLeadDays: number[];
  /** F42. Набор «за сколько дней» напоминать о годовщине рождения. */
  birthAnniversaryLeadDays: number[];
  trackedAtUtc: string;
  isVerified: boolean;
};

/**
 * Ответ GET /api/users/me/tracked-deceased/{id}/exists — backend поле Tracked.
 */
export type IsTrackedResponse = {
  tracked: boolean;
};

export const trackedDeceasedApi = {
  /**
   * GET /api/users/me/tracked-deceased?page=&pageSize= — список
   * отслеживаемых текущего юзера. Archived фильтруем на клиенте.
   */
  async list(
    page = 1,
    pageSize = 50,
  ): Promise<PagedResponse<TrackedDeceasedListItem>> {
    return unwrap(
      apiClient.get<ApiEnvelope<PagedResponse<TrackedDeceasedListItem>>>(
        '/api/users/me/tracked-deceased',
        { params: { page, pageSize } },
      ),
    );
  },

  /**
   * GET .../{id}/exists. Если упадёт по любой причине — на preview
   * считаем "не отслеживается", чтобы кнопка "Добавить" не блокировалась.
   * Backend идемпотентен — повторный track ничего не сломает.
   */
  async isTracked(deceasedId: string): Promise<IsTrackedResponse> {
    return unwrap(
      apiClient.get<ApiEnvelope<IsTrackedResponse>>(
        `/api/users/me/tracked-deceased/${deceasedId}/exists`,
      ),
    );
  },

  /**
   * POST .../{id}. Подписаться на отслеживание. Идемпотентен:
   * если юзер уже отслеживает (Active/Muted/Archived) — переводит
   * в Active.
   */
  async track(
    deceasedId: string,
    request: AddMeTrackingRequest,
  ): Promise<TrackDeceasedResponse> {
    return unwrap(
      apiClient.post<ApiEnvelope<TrackDeceasedResponse>>(
        `/api/users/me/tracked-deceased/${deceasedId}`,
        request,
      ),
    );
  },

  /**
   * DELETE .../{id} — "удалить навсегда" из tracking-списка
   * (F10 шаг 2). Сама карточка умершего НЕ удаляется — её удаляет
   * только автор или админ через отдельный endpoint. Бэк отвечает
   * HttpResponseMessage (без envelope), здесь используем apiClient
   * напрямую и проверяем статус.
   */
  async untrack(deceasedId: string): Promise<void> {
    await apiClient.delete(`/api/users/me/tracked-deceased/${deceasedId}`);
  },

  /**
   * GET .../{id} — детальная карточка отслеживаемого. Зеркало
   * MyTrackedDeceasedDetailsResponse на бэке: внутри полный DeceasedDetails +
   * MyTrackingInfo текущего юзера.
   *
   * Используется в F10 (restore: нужны tracking-поля для PATCH со всеми
   * полями сразу) и в F11 (показ всей карточки).
   */
  async getDetails(deceasedId: string): Promise<{
    deceased: DeceasedDetails;
    tracking: MyTrackingInfo;
  }> {
    return unwrap(
      apiClient.get<
        ApiEnvelope<{ deceased: DeceasedDetails; tracking: MyTrackingInfo }>
      >(`/api/users/me/tracked-deceased/${deceasedId}`),
    );
  },

  /**
   * PATCH .../{id} — обновить tracking. На бэке UpdateTrackingUseCase
   * принимает ВСЕ поля сразу. F10 (restore) перед вызовом тянет
   * MyTrackingInfo через getDetails и меняет только trackStatus.
   */
  async update(
    deceasedId: string,
    request: UpdateTrackingRequest,
  ): Promise<void> {
    await apiClient.patch(
      `/api/users/me/tracked-deceased/${deceasedId}`,
      request,
    );
  },
};
