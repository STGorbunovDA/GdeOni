import { useEffect, type ReactNode } from 'react';
import { useAuthStore } from './authStore';
import { refreshTokens } from '../api/refreshClient';
import { usersApi } from '../api/endpoints/authApi';

/**
 * F4. Startup-refresh. На монтировании:
 *  - если в store есть refresh-токен → дёргаем /api/auth/refresh,
 *    при успехе дёргаем /api/users/me для подтягивания актуальной
 *    роли (она могла измениться пока юзера не было — ChangeRole
 *    инвалидирует SecurityStamp и refresh-токен тоже становится
 *    невалидным, но если refresh успешен — роль точно актуальная);
 *  - если refresh упал или его нет → чистим store.
 *
 * isBootstrapping в store держится в true пока этот процесс идёт,
 * чтобы ProtectedRoute не редиректил преждевременно. Аналог
 * AppShell.OnAppearing.HasSessionAsync на mobile.
 *
 * Singleflight через module-level Promise: React 18 в StrictMode
 * вызывает useEffect дважды в dev, а Vite HMR может перемонтировать
 * SessionBootstrap. Шарим одну Promise, чтобы:
 *  - не делать два запроса /auth/refresh (сжирает rate-limit квоту);
 *  - и при этом обязательно дождаться завершения и снять
 *    isBootstrapping=false, даже если первое монтирование было
 *    отменено React'ом.
 */
let bootstrapPromise: Promise<void> | null = null;

async function runBootstrap(): Promise<void> {
  const { refreshToken, setTokens, setSession, clear, setBootstrapping } =
    useAuthStore.getState();

  if (!refreshToken) {
    setBootstrapping(false);
    return;
  }

  try {
    const tokens = await refreshTokens(refreshToken);
    if (!tokens) {
      clear();
      return;
    }
    // Применяем токены ДО /users/me, чтобы request interceptor
    // подложил свежий access.
    setTokens(tokens.accessToken, tokens.refreshToken);

    const me = await usersApi.me();
    setSession(tokens.accessToken, tokens.refreshToken, {
      id: me.id,
      email: me.email,
      userName: me.userName,
      fullName: me.fullName,
      role: me.role,
    });
  } catch {
    clear();
  } finally {
    setBootstrapping(false);
  }
}

export function SessionBootstrap({ children }: { children: ReactNode }) {
  useEffect(() => {
    if (!bootstrapPromise) {
      bootstrapPromise = runBootstrap();
    }
    // Не нужен cleanup: даже если компонент размонтируется (StrictMode,
    // HMR), bootstrap должен дойти до конца и снять isBootstrapping —
    // это глобальное состояние, не привязанное к жизненному циклу
    // компонента.
  }, []);

  return <>{children}</>;
}
