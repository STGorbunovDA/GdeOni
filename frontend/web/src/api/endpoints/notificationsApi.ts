import { apiClient, unwrap } from '../client';
import type { ApiEnvelope } from '../types';

/**
 * F40. Внутрисайтовые уведомления («колокольчик»). Заводит их сервер в
 * фоне (новое обращение/жалоба → админам; ответ/решение админа →
 * пользователю), клиент только читает и помечает прочитанными.
 */
export type NotificationItem = {
  id: string;
  /** Имя enum NotificationKind — по нему клиент может выбрать иконку. */
  kind: string;
  title: string;
  body: string | null;
  /** Относительный путь для перехода по клику (может быть null). */
  link: string | null;
  isRead: boolean;
  createdAtUtc: string;
};

export const notificationsApi = {
  async list(limit = 20): Promise<NotificationItem[]> {
    return unwrap(
      apiClient.get<ApiEnvelope<NotificationItem[]>>(
        `/api/notifications?limit=${limit}`,
      ),
    );
  },

  async unreadCount(): Promise<number> {
    const result = await unwrap(
      apiClient.get<ApiEnvelope<{ count: number }>>(
        '/api/notifications/unread-count',
      ),
    );
    return result.count;
  },

  // 204 No Content — тело не разворачиваем.
  async markRead(id: string): Promise<void> {
    await apiClient.post(`/api/notifications/${id}/read`);
  },

  async markAllRead(): Promise<void> {
    await apiClient.post('/api/notifications/read-all');
  },
};
