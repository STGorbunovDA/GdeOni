import { apiClient, unwrap } from '../client';
import type { ApiEnvelope } from '../types';

/**
 * F24 / D19 / D19.9. Privacy Policy и Terms of Use.
 *
 * Бэк отдаёт версию, публичный URL и сам текст (`bodyMarkdown`).
 * Канонические файлы лежат в `backend/docs/legal/*.md` и едут вместе с
 * API — web и mobile рендерят один и тот же текст и своей копии не
 * держат. Раньше текст жил в бандле web, а версия — в appsettings бэка:
 * два места, которые обязаны совпадать, но ничем не были связаны.
 */
export type LegalDocument = {
  documentKey: string;
  version: number;
  url: string;
  /** Markdown-текст документа. Null, если файл не доехал до сервера. */
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
