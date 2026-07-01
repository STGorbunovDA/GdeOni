import { apiClient, unwrap } from '../client';
import type { ApiEnvelope, PagedResponse } from '../types';

/**
 * F17.8 / D23. Админский аудит платежей подписок. GET
 * /api/admin/payments — единственный эндпоинт: пагинация + фильтры.
 * Никаких mutation'ов — платежи менять нельзя, они приходят через
 * YooKassa webhook и отражают внешнее состояние.
 */

/**
 * Зеркало backend PaymentRecordStatus enum. Pending — ждём webhook,
 * Succeeded — подтверждён и подписка активирована, Cancelled — юзер
 * отменил на стороне YooKassa или таймаут confirmation_url, Failed —
 * YooKassa вернула ошибку или банк отклонил.
 */
export type PaymentStatus = 'Pending' | 'Succeeded' | 'Cancelled' | 'Failed';

/** Зеркало PaymentRecordResponse. */
export type PaymentRecord = {
  id: string;
  userId: string;
  userEmail: string | null;
  externalPaymentId: string;
  plan: string;
  amountRub: number;
  status: PaymentStatus;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  periodStartUtc: string | null;
  periodEndUtc: string | null;
};

export type ListAdminPaymentsParams = {
  emailSearch?: string;
  status?: PaymentStatus;
  createdFromUtc?: string;
  createdToUtc?: string;
  page: number;
  pageSize: number;
};

export const adminPaymentsApi = {
  async list(
    params: ListAdminPaymentsParams,
  ): Promise<PagedResponse<PaymentRecord>> {
    // Backend отвечает { items, totalCount, page, pageSize } — тот же
    // shape что PagedResponse, но camelCase имя поля `items` совпадает
    // (backend возвращает { items } через SnakeCase-конвертер JSON).
    return unwrap(
      apiClient.get<ApiEnvelope<PagedResponse<PaymentRecord>>>(
        '/api/admin/payments',
        { params },
      ),
    );
  },
};
