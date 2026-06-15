import { create } from 'zustand';
import { persist } from 'zustand/middleware';

/**
 * F1 / F2.1 / F4. Auth store на Zustand.
 *
 * F4 расширил поля на полный профиль (email, userName, fullName,
 * userId) — приходят сразу из /api/auth/login (см. LoginResponse).
 * GET /users/me для подтягивания этих данных делать НЕ надо.
 *
 * Persist в localStorage — осознанный XSS-trade-off (см. план F4
 * комментарий про httpOnly cookies). На production уйдём на
 * cookie-based refresh, когда добавим SSR/прокси-эндпойнт.
 */
export type UserRole = 'User' | 'Admin' | 'SuperAdmin';

export type AuthUser = {
  id: string;
  email: string;
  userName: string;
  fullName: string | null;
  role: UserRole;
};

type AuthState = {
  accessToken: string | null;
  refreshToken: string | null;
  user: AuthUser | null;
  /**
   * Pending startup-refresh: пока true, ProtectedRoute НЕ редиректит
   * на /login — даёт шанс восстановить сессию из localStorage. См.
   * src/auth/SessionBootstrap.tsx.
   */
  isBootstrapping: boolean;
  setSession: (access: string, refresh: string, user: AuthUser) => void;
  setTokens: (access: string, refresh: string) => void;
  setBootstrapping: (v: boolean) => void;
  clear: () => void;
};

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      user: null,
      isBootstrapping: true,
      setSession: (access, refresh, user) =>
        set({ accessToken: access, refreshToken: refresh, user }),
      setTokens: (access, refresh) =>
        set({ accessToken: access, refreshToken: refresh }),
      setBootstrapping: (v) => set({ isBootstrapping: v }),
      clear: () =>
        set({ accessToken: null, refreshToken: null, user: null }),
    }),
    {
      name: 'gdeoni-auth',
      // isBootstrapping не персистим — при каждом запуске начинается
      // заново.
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        user: state.user,
      }),
    },
  ),
);

export const useIsAuthenticated = () =>
  useAuthStore((s) => s.accessToken !== null);

export const useIsAdmin = () =>
  useAuthStore((s) => s.user?.role === 'Admin' || s.user?.role === 'SuperAdmin');
