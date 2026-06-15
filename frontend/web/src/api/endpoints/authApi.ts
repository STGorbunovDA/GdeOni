import { apiClient, unwrap } from '../client';
import type {
  ApiEnvelope,
  AuthTokensResponse,
  CurrentUserSummary,
} from '../types';

/**
 * F3. Минимальный auth API. Реальный UI login появится в F4 — там же
 * добавится /api/auth/register.
 */

export const authApi = {
  /**
   * POST /api/auth/login. Возвращает access + refresh.
   * F4 будет хранить токены в Zustand store через authStore.setSession().
   */
  async login(email: string, password: string): Promise<AuthTokensResponse> {
    return unwrap(
      apiClient.post<ApiEnvelope<AuthTokensResponse>>('/api/auth/login', {
        email,
        password,
      }),
    );
  },

  /**
   * POST /api/auth/logout. Best-effort: сервер инвалидирует refresh,
   * но клиент в любом случае чистит локальное состояние. Ошибки не
   * критичны (например, токен уже истёк).
   */
  async logout(): Promise<void> {
    try {
      await apiClient.post('/api/auth/logout');
    } catch {
      // Игнорируем — клиент всё равно вычистит local state.
    }
  },
};

/**
 * GET /api/users/me — минимальная сводка по текущему юзеру.
 * Используется F3 для проверки авторизации и F4 для заполнения профиля
 * сразу после логина.
 */
export const userApi = {
  async me(): Promise<CurrentUserSummary> {
    return unwrap(
      apiClient.get<ApiEnvelope<CurrentUserSummary>>('/api/users/me'),
    );
  },
};
