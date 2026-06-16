import { apiClient, unwrap } from '../client';
import type { ApiEnvelope } from '../types';

/**
 * D36 / F22. /api/app/features — глобальные флаги + базовый URL медиа.
 * Дёргается один раз при логине и кешируется в TanStack Query.
 */
export type AppFeatures = {
  subscriptionEnabled: boolean;
  gracePeriodDaysAfterExpiry: number;
  /**
   * D36. Базовый URL хранилища (без trailing slash). Клиент строит
   * полный URL картинки через ${mediaBaseUrl}/${bucket}/${encodeURIComponent(key)}.
   */
  mediaBaseUrl: string;
};

export const appApi = {
  async features(): Promise<AppFeatures> {
    return unwrap(
      apiClient.get<ApiEnvelope<AppFeatures>>('/api/app/features'),
    );
  },
};
