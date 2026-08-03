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

/** Новый родственник для попапа «События» (Фаза 4). */
export type NewRelativeSummaryItem = {
  deceasedId: string;
  deceasedFullName: string;
  relativeUserId: string;
  relativeUserName: string;
  /** Имя связи (enum) — рендерим через relationshipDisplay. */
  relationshipType: string;
};

/** Диалог с непрочитанными сообщениями (Фаза 4). */
export type UnreadConversationItem = {
  conversationId: string;
  deceasedId: string;
  deceasedFullName: string;
  otherUserId: string;
  otherUserName: string;
  unreadCount: number;
};

/** Сводка «Родственников»: для попапа «События» и бейджа вкладки (Фаза 4). */
export type RelativesSummary = {
  newRelatives: NewRelativeSummaryItem[];
  unreadConversations: UnreadConversationItem[];
  totalUnreadMessages: number;
};

/** Одно сообщение в переписке (Фаза 3). */
export type RelativeMessage = {
  id: string;
  isMine: boolean;
  text: string;
  createdAtUtc: string;
  editedAtUtc: string | null;
  isRead: boolean;
  /** true только у своего последнего сообщения, пока собеседник не ответил. */
  canEditDelete: boolean;
};

/** Экран диалога. */
export type RelativeConversationDetail = {
  conversationId: string;
  deceasedId: string;
  deceasedFullName: string;
  otherUserId: string;
  otherUserName: string;
  otherRelationship: string | null;
  /** Можно ли сейчас отправить (твой ход). */
  canSend: boolean;
  messages: RelativeMessage[];
};

/** Строка списка диалогов (инбокс + непрочитанные). */
export type RelativeConversationSummary = {
  conversationId: string;
  deceasedId: string;
  deceasedFullName: string;
  otherUserId: string;
  otherUserName: string;
  otherRelationship: string | null;
  lastMessageAtUtc: string;
  lastMessagePreview: string | null;
  lastMessageIsMine: boolean;
  unreadCount: number;
  canSend: boolean;
};

export const relativesApi = {
  /** GET /api/relatives — список «родственников» текущего пользователя. */
  async myRelatives(): Promise<MyRelativeItem[]> {
    const res = await unwrap(
      apiClient.get<ApiEnvelope<{ items: MyRelativeItem[] }>>('/api/relatives'),
    );
    return res.items;
  },

  /** GET /api/relatives/summary — новые родственники + непрочитанные (Фаза 4). */
  async getSummary(): Promise<RelativesSummary> {
    return unwrap(
      apiClient.get<ApiEnvelope<RelativesSummary>>('/api/relatives/summary'),
    );
  },

  /** POST /api/relatives/seen — отметить новых родственников просмотренными. */
  async markRelativesSeen(): Promise<void> {
    await unwrap(apiClient.post<ApiEnvelope<unknown>>('/api/relatives/seen'));
  },

  /** POST /api/relatives/conversations — открыть/получить диалог с родственником. */
  async startConversation(
    deceasedId: string,
    otherUserId: string,
  ): Promise<RelativeConversationDetail> {
    return unwrap(
      apiClient.post<ApiEnvelope<RelativeConversationDetail>>(
        '/api/relatives/conversations',
        { deceasedId, otherUserId },
      ),
    );
  },

  /** GET /api/relatives/conversations/{id} — детали диалога (отмечает прочитанным). */
  async getConversation(id: string): Promise<RelativeConversationDetail> {
    return unwrap(
      apiClient.get<ApiEnvelope<RelativeConversationDetail>>(
        `/api/relatives/conversations/${id}`,
      ),
    );
  },

  /** GET /api/relatives/conversations — список диалогов. */
  async listConversations(): Promise<RelativeConversationSummary[]> {
    const res = await unwrap(
      apiClient.get<ApiEnvelope<{ items: RelativeConversationSummary[] }>>(
        '/api/relatives/conversations',
      ),
    );
    return res.items;
  },

  /** POST .../messages — отправить (только когда твой ход, иначе 409). */
  async sendMessage(
    id: string,
    text: string,
  ): Promise<RelativeConversationDetail> {
    return unwrap(
      apiClient.post<ApiEnvelope<RelativeConversationDetail>>(
        `/api/relatives/conversations/${id}/messages`,
        { text },
      ),
    );
  },

  /** PATCH .../messages/{messageId} — изменить своё последнее сообщение. */
  async editMessage(
    id: string,
    messageId: string,
    text: string,
  ): Promise<RelativeConversationDetail> {
    return unwrap(
      apiClient.patch<ApiEnvelope<RelativeConversationDetail>>(
        `/api/relatives/conversations/${id}/messages/${messageId}`,
        { text },
      ),
    );
  },

  /** DELETE .../messages/{messageId} — удалить своё последнее сообщение. */
  async deleteMessage(
    id: string,
    messageId: string,
  ): Promise<RelativeConversationDetail> {
    return unwrap(
      apiClient.delete<ApiEnvelope<RelativeConversationDetail>>(
        `/api/relatives/conversations/${id}/messages/${messageId}`,
      ),
    );
  },

  /**
   * POST /api/relatives/reports — пожаловаться на собеседника (Фаза 5).
   * created=false — активная жалоба на него в этом диалоге уже была.
   */
  async report(
    conversationId: string,
    reason: string,
  ): Promise<{ created: boolean }> {
    return unwrap(
      apiClient.post<ApiEnvelope<{ created: boolean }>>(
        '/api/relatives/reports',
        { conversationId, reason },
      ),
    );
  },
};

/** Жалоба на родственника в админском списке (Фаза 5). */
export type AdminRelativeReport = {
  id: string;
  reporterUserId: string;
  reporterUserName: string;
  reportedUserId: string;
  reportedUserName: string;
  reportedIsBlocked: boolean;
  deceasedId: string;
  deceasedFullName: string;
  conversationId: string | null;
  reason: string;
  createdAtUtc: string;
  /** 'Pending' | 'Resolved'. */
  status: string;
  resolvedAtUtc: string | null;
  resolutionNote: string | null;
};

export const adminRelativeReportsApi = {
  /** GET /api/admin/relative-reports?pendingOnly= — список жалоб. */
  async list(pendingOnly: boolean): Promise<AdminRelativeReport[]> {
    const res = await unwrap(
      apiClient.get<ApiEnvelope<{ items: AdminRelativeReport[] }>>(
        '/api/admin/relative-reports',
        { params: { pendingOnly } },
      ),
    );
    return res.items;
  },

  /** POST /api/admin/relative-reports/{id}/resolve — пометить разобранной. */
  async resolve(id: string, note: string | null): Promise<void> {
    await unwrap(
      apiClient.post<ApiEnvelope<unknown>>(
        `/api/admin/relative-reports/${id}/resolve`,
        { note },
      ),
    );
  },
};
