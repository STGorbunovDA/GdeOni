import axios from 'axios';
import { API_BASE_URL } from './config';
import type { ApiEnvelope, RefreshResponse } from './types';

/**
 * F3. Отдельный axios-instance исключительно для POST /api/auth/refresh.
 * НЕТ interceptors — иначе при провале refresh interceptor попытался бы
 * сделать ещё один refresh, и так до StackOverflow. Mobile-аналог —
 * RefreshHttpClientProvider в AuthTokenHandler.
 */
const refreshAxios = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30_000,
  headers: { 'Content-Type': 'application/json' },
});

export async function refreshTokens(
  refreshToken: string,
): Promise<RefreshResponse | null> {
  try {
    const { data } = await refreshAxios.post<ApiEnvelope<RefreshResponse>>(
      '/api/auth/refresh',
      { refreshToken },
    );
    return data.result ?? null;
  } catch {
    return null;
  }
}
