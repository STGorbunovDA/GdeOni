import { apiClient, unwrap } from '../client';
import type { ApiEnvelope } from '../types';

/**
 * D46. «Поделиться подборкой карточек».
 *
 * Отправитель выбирает свои отслеживаемые карточки → create() отдаёт
 * короткий код; полную ссылку/QR клиент строит от своего origin
 * (`{origin}/s/{code}`). Получатель по ссылке видит список (get) и
 * добавляет к себе (import). Публичной страницы нет — все три требуют входа.
 */
export type CreateShareBundleResult = {
  code: string;
  expiresAtUtc: string;
};

export type ShareBundleItem = {
  deceasedId: string;
  fullName: string;
  birthDate: string | null;
  deathDate: string;
  country: string | null;
  city: string | null;
  cemeteryName: string | null;
  /**
   * Статус этой карточки у текущего получателя: null — не отслеживает
   * (будет добавлена), иначе 'Active' | 'Muted' | 'Archived' (уже есть,
   * импорт её не трогает). D46 follow-up.
   */
  trackingStatus: string | null;
};

export type ShareBundleResult = {
  items: ShareBundleItem[];
  expiresAtUtc: string;
};

export type ImportShareBundleResult = {
  added: number;
  skipped: number;
  total: number;
};

export const shareApi = {
  /** POST /api/share-bundles — создать подборку из id выбранных карточек. */
  async create(deceasedIds: string[]): Promise<CreateShareBundleResult> {
    return unwrap(
      apiClient.post<ApiEnvelope<CreateShareBundleResult>>(
        '/api/share-bundles',
        { deceasedIds },
      ),
    );
  },

  /** GET /api/share-bundles/{code} — раскрыть подборку по коду (строки). */
  async get(code: string): Promise<ShareBundleResult> {
    return unwrap(
      apiClient.get<ApiEnvelope<ShareBundleResult>>(
        `/api/share-bundles/${encodeURIComponent(code)}`,
      ),
    );
  },

  /**
   * POST /api/share-bundles/{code}/import — добавить всю подборку в своё
   * отслеживание. Под подпиской: без активной — 403 subscription.required,
   * axios-интерсептор уведёт на paywall.
   */
  async import(code: string): Promise<ImportShareBundleResult> {
    return unwrap(
      apiClient.post<ApiEnvelope<ImportShareBundleResult>>(
        `/api/share-bundles/${encodeURIComponent(code)}/import`,
        {},
      ),
    );
  },
};
