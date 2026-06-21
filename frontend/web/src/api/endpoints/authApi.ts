import { apiClient, unwrap } from '../client';
import type {
  ApiEnvelope,
  CurrentUserResponse,
  LoginResponse,
  RegisterResponse,
} from '../types';
import { useAuthStore } from '../../auth/authStore';

/**
 * F4. Auth-эндпоинты + helper'ы для логина/регистрации/logout.
 */
export const authApi = {
  /**
   * POST /api/auth/login. Возвращает полный профиль + пару токенов.
   * Отдельно дёргать /users/me НЕ надо — все нужные поля уже здесь.
   */
  async login(email: string, password: string): Promise<LoginResponse> {
    return unwrap(
      apiClient.post<ApiEnvelope<LoginResponse>>('/api/auth/login', {
        email,
        password,
      }),
    );
  },

  /**
   * POST /api/auth/logout. Best-effort: сервер отзывает refresh,
   * клиент в любом случае чистит локальный state. Принимает текущий
   * refresh-токен в body (бэк проверяет принадлежность юзеру).
   */
  async logout(): Promise<void> {
    const refresh = useAuthStore.getState().refreshToken;
    if (!refresh) return;
    try {
      await apiClient.post('/api/auth/logout', { refreshToken: refresh });
    } catch {
      // Игнорируем — клиент всё равно чистит local state.
    }
  },
};

/**
 * D19 / F24. POST /api/users — регистрация. Бэк требует
 * privacyPolicyAccepted и termsAccepted (152-ФЗ): без галочки
 * use case вернёт ошибку валидации. Возвращает только Id —
 * сразу после успеха делаем login для получения токенов.
 */
export const usersApi = {
  async register(input: {
    email: string;
    password: string;
    userName?: string;
    fullName?: string;
  }): Promise<RegisterResponse> {
    return unwrap(
      apiClient.post<ApiEnvelope<RegisterResponse>>('/api/users', {
        email: input.email,
        password: input.password,
        userName: input.userName,
        fullName: input.fullName,
        privacyPolicyAccepted: true,
        termsAccepted: true,
      }),
    );
  },

  /**
   * GET /api/users/me. Используется при startup-refresh для
   * подтягивания актуальной роли/legal-флагов.
   */
  async me(): Promise<CurrentUserResponse> {
    return unwrap(
      apiClient.get<ApiEnvelope<CurrentUserResponse>>('/api/users/me'),
    );
  },

  /**
   * F16. PUT /api/users/{userId}/password. Бэк ротирует SecurityStamp,
   * текущий access-токен умрёт через TTL ~30s (см. F4 OnTokenValidated).
   * После успеха клиент сам делает force-logout, чтобы не показывать
   * полминуты протухший UI и не словить 401 на следующем запросе.
   *
   * Rate-limited на бэке через AuthRateLimitOptions — 429 поднимается
   * наверх и переводится в человеческое сообщение в formatError.
   */
  async changePassword(
    userId: string,
    input: { currentPassword: string; newPassword: string },
  ): Promise<void> {
    await unwrap(
      apiClient.put<ApiEnvelope<{ userId: string }>>(
        `/api/users/${userId}/password`,
        input,
      ),
    );
  },
};
