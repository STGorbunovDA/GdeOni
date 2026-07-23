import { apiClient, unwrap } from '../client';
import type { ApiEnvelope, PagedResponse } from '../types';

/**
 * F17.9 / D24. Аудит правок карточек умерших. Два эндпоинта:
 *  - GET /api/admin/edits — глобальная лента с фильтрами по deceasedId,
 *    editorUserId, диапазону EditedAt;
 *  - GET /api/deceased-records/{id}/edits — история одной карточки.
 *
 * Диапазон дат и пагинация валидируются на бэке (400 при невалидных
 * значениях), UI не делает inline clamping.
 */

/**
 * Зеркало DeceasedEditKind: MainInfo (1) / Metadata (2) /
 * BurialLocation (3) / Reassignment (4). JSON serializer на бэке
 * настроен как StringEnum, так что приходит именно имя.
 */
export type EditKind =
  | 'MainInfo'
  | 'Metadata'
  | 'BurialLocation'
  | 'Reassignment';

/**
 * Запись правки для глобальной ленты. Отличается от per-card наличием
 * DeceasedId + DeceasedFullName — их нужно показывать колонкой,
 * потому что в глобальной ленте карточки разные.
 */
export type EditWithCard = {
  id: string;
  editedAtUtc: string;
  deceasedId: string;
  deceasedFullName: string;
  editedByUserId: string | null;
  editedByEmail: string | null;
  editedByDisplayName: string | null;
  kind: EditKind;
  /**
   * JSON вида { "FieldName": { "old": "...", "new": "..." }, ... }.
   * Парсится UI-модалью в side-by-side diff (backend/DeceasedEdit.cs
   * задаёт этот shape).
   */
  changesJson: string;
};

/** Per-card запись — без DeceasedId/FullName (карточка уже понятна из URL). */
export type EditItem = {
  id: string;
  editedAtUtc: string;
  editedByUserId: string | null;
  editedByEmail: string | null;
  editedByDisplayName: string | null;
  kind: EditKind;
  changesJson: string;
};

export type ListAllEditsParams = {
  deceasedId?: string;
  editorUserId?: string;
  editedFromUtc?: string;
  editedToUtc?: string;
  page: number;
  pageSize: number;
};

export const adminEditsApi = {
  async listAll(
    params: ListAllEditsParams,
  ): Promise<PagedResponse<EditWithCard>> {
    return unwrap(
      apiClient.get<ApiEnvelope<PagedResponse<EditWithCard>>>(
        '/api/admin/edits',
        { params },
      ),
    );
  },

  async listByDeceased(
    deceasedId: string,
    page: number,
    pageSize: number,
  ): Promise<PagedResponse<EditItem>> {
    return unwrap(
      apiClient.get<ApiEnvelope<PagedResponse<EditItem>>>(
        `/api/deceased-records/${deceasedId}/edits`,
        { params: { page, pageSize } },
      ),
    );
  },
};
