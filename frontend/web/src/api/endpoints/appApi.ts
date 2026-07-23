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
  /**
   * F39. Цена месячной подписки в рублях — из того же конфига, откуда её
   * берёт создание платежа. Раньше UI писал «49 ₽» текстом, и смена тарифа
   * означала бы: на кнопке одна сумма, а спишется другая.
   */
  monthlyPriceRub: number;
  /**
   * D44. Настроен ли настоящий платёжный провайдер. Если false —
   * онлайн-оплата недоступна (на бэке работает заглушка, её checkout-URL
   * ведёт в никуда). Клиент обязан гасить кнопку «Оформить подписку» и
   * предлагать написать обращение — оплату проводим переводом вручную.
   */
  paymentsAvailable: boolean;
};

/**
 * D17 / F22. /api/app/version — минимально-поддерживаемая и последняя
 * версия клиента. AllowAnonymous на бэке — работает без токена.
 */
export type AppVersion = {
  minSupportedVersion: string;
  latestVersion: string;
  forceUpdateMessage: string | null;
  downloadUrl: string | null;
};

export const appApi = {
  async features(): Promise<AppFeatures> {
    return unwrap(
      apiClient.get<ApiEnvelope<AppFeatures>>('/api/app/features'),
    );
  },

  async version(): Promise<AppVersion> {
    return unwrap(
      apiClient.get<ApiEnvelope<AppVersion>>('/api/app/version'),
    );
  },
};
