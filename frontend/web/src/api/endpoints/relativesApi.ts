import { apiClient, unwrap } from '../client';
import type { ApiEnvelope } from '../types';

/**
 * Функция «Родственники». Список людей, отслеживающих те же карточки, что и
 * текущий пользователь, со связывающим родством и включённым согласием.
 * Почта не приходит — переписка внутренняя (Фаза 3).
 */
export type MyRelativeItem = {
  deceasedId: string;
  deceasedFullName: string;
  birthDate: string | null;
  deathDate: string;
  relativeUserId: string;
  relativeUserName: string;
  /** Имя связи (enum): Mother/Father/Friend/... — рендерим через relationshipDisplay. */
  relationshipType: string;
};

export const relativesApi = {
  /** GET /api/relatives — список «родственников» текущего пользователя. */
  async myRelatives(): Promise<MyRelativeItem[]> {
    const res = await unwrap(
      apiClient.get<ApiEnvelope<{ items: MyRelativeItem[] }>>('/api/relatives'),
    );
    return res.items;
  },
};
