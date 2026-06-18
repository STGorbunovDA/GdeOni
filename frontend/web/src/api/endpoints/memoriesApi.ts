import { apiClient, unwrap } from '../client';
import type { ApiEnvelope } from '../types';

/**
 * F12. Эндпоинты CRUD воспоминаний. GET-list НЕТ — memories приходят
 * вместе с DeceasedDetails (см. deceasedApi.getById /
 * trackedDeceasedApi.getDetails) ради избежания N+1 запросов.
 *
 * После POST/PUT/DELETE клиент инвалидирует details-query, и
 * Memories перетягиваются вместе со всей карточкой.
 */

/** Максимальная длина текста воспоминания (зеркало DeceasedMemoryEntry.MaxTextLength). */
export const MEMORY_TEXT_MAX_LENGTH = 5000;

type AddMemoryResponse = {
  memoryId: string;
};

type UpdateMemoryResponse = {
  memoryId: string;
};

type RemoveMemoryResponse = {
  memoryId: string;
};

export const memoriesApi = {
  /**
   * POST /api/deceased-records/{deceasedId}/memories — создать
   * воспоминание. Автор — текущий юзер (берётся из JWT на бэке).
   */
  async add(deceasedId: string, text: string): Promise<AddMemoryResponse> {
    return unwrap(
      apiClient.post<ApiEnvelope<AddMemoryResponse>>(
        `/api/deceased-records/${deceasedId}/memories`,
        { text },
      ),
    );
  },

  /**
   * PUT /api/deceased-records/{deceasedId}/memories/{memoryId} —
   * редактировать своё воспоминание. 403 если автор не текущий юзер.
   */
  async update(
    deceasedId: string,
    memoryId: string,
    text: string,
  ): Promise<UpdateMemoryResponse> {
    return unwrap(
      apiClient.put<ApiEnvelope<UpdateMemoryResponse>>(
        `/api/deceased-records/${deceasedId}/memories/${memoryId}`,
        { text },
      ),
    );
  },

  /**
   * DELETE /api/deceased-records/{deceasedId}/memories/{memoryId} —
   * удалить своё воспоминание. 403 если автор не текущий юзер
   * (админы тоже могут — модерация F17).
   */
  async remove(deceasedId: string, memoryId: string): Promise<RemoveMemoryResponse> {
    return unwrap(
      apiClient.delete<ApiEnvelope<RemoveMemoryResponse>>(
        `/api/deceased-records/${deceasedId}/memories/${memoryId}`,
      ),
    );
  },
};
