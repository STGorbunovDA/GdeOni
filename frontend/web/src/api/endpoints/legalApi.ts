import { apiClient, unwrap } from '../client';
import type { ApiEnvelope } from '../types';

/**
 * F24 / D19. Privacy Policy и Terms of Use.
 *
 * Бэк отдаёт только метаданные (версия + публичный URL); сам текст
 * лежит статикой на клиенте (`src/pages/legal/*.md?raw`) — так
 * документ едет в бандл без отдельного хостинга. Версии клиента
 * и бэка должны совпадать при <c>accept()</c>.
 */
export type LegalDocument = {
  documentKey: string;
  version: number;
  url: string;
  bodyMarkdown: string | null;
};

export const legalApi = {
  async getPrivacyPolicy(): Promise<LegalDocument> {
    return unwrap(
      apiClient.get<ApiEnvelope<LegalDocument>>('/api/legal/privacy-policy'),
    );
  },

  async getTermsOfUse(): Promise<LegalDocument> {
    return unwrap(
      apiClient.get<ApiEnvelope<LegalDocument>>('/api/legal/terms-of-use'),
    );
  },

  /**
   * POST /api/users/me/accept-legal. Юзер подтверждает, что прочёл
   * текущие версии обоих документов. Обе версии должны быть равны
   * серверной — иначе 409 legal.version.outdated (значит, пока юзер
   * читал модалку, юрист выкатил ещё одну версию). Обрабатывается
   * повторным показом модалки с обновлёнными версиями.
   */
  async accept(input: {
    privacyPolicyVersion: number;
    termsVersion: number;
  }): Promise<void> {
    await apiClient.post('/api/users/me/accept-legal', input);
  },
};
