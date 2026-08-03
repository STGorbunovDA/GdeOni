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
};
