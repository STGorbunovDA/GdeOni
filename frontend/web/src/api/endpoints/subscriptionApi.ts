import { apiClient, unwrap } from '../client';
import type { ApiEnvelope } from '../types';

/**
 * F22 / D16. Подписка пользователя. Endpoint'ы под
 * `/api/users/me/subscription` — userId берётся из JWT на бэке.
 * Все три эндпоинта в whitelist серверного paywall'а (BasicAuthenticated),
 * чтобы юзер без активной подписки мог оформить.
 */

/** Строковое значение SubscriptionStatus (см. Domain/Shared/SubscriptionStatus.cs). */
export type SubscriptionStatus =
  | 'None'
  | 'Trial'
  | 'PendingPayment'
  | 'Active'
  | 'Cancelled'
  | 'Expired';

/** Строковое значение SubscriptionPlan (D16, план 2026-05-14: только Monthly). */
export type SubscriptionPlan = 'Monthly';

export type MySubscription = {
  status: SubscriptionStatus;
  plan: SubscriptionPlan | null;
  expiresAtUtc: string | null;
  cancelledAtUtc: string | null;
  /**
   * D22. true, если у юзера активна подписка ИЛИ выдан complimentary
   * доступ. UI и <RequireSubscription> опираются именно на это поле.
   */
  isActiveNow: boolean;
  isOnTrial: boolean;
  /** Округлено вверх. 0 если истекло. */
  daysUntilExpiry: number;
  hasComplimentaryAccess: boolean;
  complimentaryAccessUntilUtc: string | null;
  complimentaryAccessNote: string | null;
};

export type CreatePaymentResponse = {
  checkoutUrl: string;
  externalPaymentId: string;
};

export const subscriptionApi = {
  /**
   * GET /api/users/me/subscription. Возвращает 404 если у юзера
   * нет записи (можно случиться только для админов, которых
   * никогда не переводили в Trial). UI трактует 404 как "нет
   * подписки, показывать paywall не надо, если ты админ".
   */
  async getMy(): Promise<MySubscription> {
    return unwrap(
      apiClient.get<ApiEnvelope<MySubscription>>(
        '/api/users/me/subscription',
      ),
    );
  },

  /**
   * POST /api/users/me/subscription/create-payment. Возвращает URL
   * платёжной страницы YooKassa — фронт должен редиректнуть на неё
   * через `window.location.href`. После оплаты YooKassa вернёт юзера
   * на /payment/return (см. YooKassaOptions.ReturnUrl на бэке).
   */
  async createPayment(plan: SubscriptionPlan): Promise<CreatePaymentResponse> {
    return unwrap(
      apiClient.post<ApiEnvelope<CreatePaymentResponse>>(
        '/api/users/me/subscription/create-payment',
        { plan },
      ),
    );
  },

  /**
   * POST /api/users/me/subscription/cancel. Оставляет ExpiresAtUtc —
   * paid-period дорабатывает, автопродления не будет. 204 No Content.
   */
  async cancel(): Promise<void> {
    await apiClient.post('/api/users/me/subscription/cancel');
  },
};
