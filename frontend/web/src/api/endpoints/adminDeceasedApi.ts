import { apiClient, unwrap } from '../client';
import type { ApiEnvelope, PagedResponse } from '../types';

/**
 * F17.1. Админ-API по карточкам умерших. Реально GET /api/deceased-records
 * после D15 открыт всем authenticated юзерам (нужно для поиска перед
 * созданием карточки), но в админке мы используем те же эндпоинты с
 * расширенными фильтрами (IsVerified, CreatedFrom/CreatedTo).
 */
export type AdminDeceasedListItem = {
  id: string;
  fullName: string;
  birthDate: string | null;
  deathDate: string;
  hasBurialLocation: boolean;
  country: string | null;
  city: string | null;
  cemeteryName: string | null;
  isVerified: boolean;
  createdAtUtc: string;
  createdByUserId: string;
  createdByUserName: string | null;
  mainPhotoBucket: string | null;
  mainPhotoStorageKey: string | null;
};

export type ListAdminDeceasedParams = {
  search?: string;
  country?: string;
  city?: string;
  isVerified?: boolean;
  createdFrom?: string;
  createdTo?: string;
  page: number;
  pageSize: number;
};

export const adminDeceasedApi = {
  /**
   * GET /api/deceased-records. На бэке использует pg_trgm GIN-индексы
   * для ILike — поиск по подстроке быстрый даже на сотнях тысяч карточек.
   */
  async list(
    params: ListAdminDeceasedParams,
  ): Promise<PagedResponse<AdminDeceasedListItem>> {
    return unwrap(
      apiClient.get<ApiEnvelope<PagedResponse<AdminDeceasedListItem>>>(
        '/api/deceased-records',
        { params },
      ),
    );
  },
};
